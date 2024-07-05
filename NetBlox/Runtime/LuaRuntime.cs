﻿using MoonSharp.Interpreter;
using NetBlox.Instances;
using NetBlox.Instances.Scripts;
using NetBlox.Instances.Services;
using NetBlox.Structs;
using System.ComponentModel;
using System.Numerics;
using System.Reflection;
using System.Runtime;
using System.Text;
using System.Threading;
using Script = MoonSharp.Interpreter.Script;

namespace NetBlox.Runtime
{
	public static class LuaRuntime
	{
		public static Exception? LastException;
		public static int ScriptExecutionTimeout = 700;
		private static Type dvt = typeof(DynValue);

		public static void Setup(GameManager gm)
		{
			Script tenv;
			tenv = new Script(
				CoreModules.Basic | CoreModules.Metatables | CoreModules.Bit32 | CoreModules.Coroutine |
				CoreModules.TableIterators | CoreModules.Table | CoreModules.String | CoreModules.ErrorHandling |
				CoreModules.Math | CoreModules.OS_Time | CoreModules.GlobalConsts);
			gm.MainEnvironment = tenv;

			tenv.Globals["game"] = MakeInstanceTable(gm.CurrentRoot, gm);

			tenv.Globals["wait"] = DynValue.NewCallback((x, y) =>
			{
				var wa = y.Count == 0 ? DateTime.Now : DateTime.Now.AddSeconds(y[0].Number);
				TaskScheduler.CurrentJob.JoinedUntil = wa;
				return DynValue.NewYieldReq([]); // here we go to the next, bc thread is paused
			});
			tenv.Globals["require"] = DynValue.NewCallback((x, y) =>
			{
				var table = y[0];
				var inst = SerializationManager.LuaDeserialize<Instance>(table, gm);

				var ms = inst as ModuleScript;
				if (ms == null)
					throw new Exception("Expected a ModuleScript");

				if (gm.LoadedModules.TryGetValue(ms, out var dv)) return dv;

				var lt = TaskScheduler.CurrentJob;
				lt.JoinedTo = TaskScheduler.ScheduleScript(gm, ms.Source, (int)lt.AssociatedObject1, ms, x =>
				{
					DynValue dv = (DynValue)x.AssociatedObject5;
					lt.AssociatedObject4 = dv.Type == DataType.Tuple ? dv.Tuple : [dv];
					return JobResult.CompletedSuccess;
				});

				return DynValue.NewYieldReq([]);
			});
			tenv.Globals["printidentity"] = DynValue.NewCallback((x, y) =>
			{
				if (y.Count != 0)
				{
					string prefix = y.AsStringUsingMeta(x, 0, "printidentity");
					PrintOut(prefix + " " + (int)TaskScheduler.CurrentJob.AssociatedObject1);
				}
				else
					PrintOut("Current identity is " + (int)TaskScheduler.CurrentJob.AssociatedObject1);
				return DynValue.Void;
			});
			tenv.Globals["print"] = DynValue.NewCallback((x, y) =>
			{
				List<string> strs = [];
				for (int i = 0; i < y.Count; i++)
					strs.Add(y.AsStringUsingMeta(x, i, "print"));
				PrintOut(string.Join(' ', strs));
				return DynValue.Void;
			});
			tenv.Globals["warn"] = DynValue.NewCallback((x, y) =>
			{
				List<string> strs = [];
				for (int i = 0; i < y.Count; i++)
					strs.Add(y.AsStringUsingMeta(x, i, "warn"));
				PrintWarn(string.Join(' ', strs));
				return DynValue.Void;
			});
			tenv.Globals["error"] = DynValue.NewCallback((x, y) =>
			{
				List<string> strs = [];
				for (int i = 0; i < y.Count; i++)
					strs.Add(y.AsStringUsingMeta(x, i, "error"));
				PrintError(string.Join(' ', strs));
				throw new Exception(y[0].ToString());
			});
			tenv.Globals["spawn"] = DynValue.NewCallback((x, y) =>
			{
				TaskScheduler.ScheduleScript(gm, y[0], 8, null);
				return DynValue.Void;
			});

			MakeDataType(gm, "Instance", (x, y) =>
			{
				try
				{
					var inst = InstanceCreator.CreateAccessibleInstance(y[0].String, gm);
					return DynValue.NewTable(MakeInstanceTable(inst, gm));
				}
				catch
				{
					return DynValue.Void;
				}
			});
			MakeDataType(gm, "UDim2", (x, y) =>
			{
				try
				{
					return SerializationManager.LuaSerializers["NetBlox.Structs.UDim2"]
						(new UDim2(
							Convert.ToSingle(y[0].Number), 
							Convert.ToSingle(y[1].Number), 
							Convert.ToSingle(y[2].Number), 
							Convert.ToSingle(y[3].Number)), gm);
				}
				catch
				{
					return DynValue.Void;
				}
			});
			MakeDataType(gm, "Color3", (x, y) =>
			{
				try
				{
					return SerializationManager.LuaSerializers["Raylib_cs.Color"]
						(new Color(
							Convert.ToInt32(y[0].Number * 255),
							Convert.ToInt32(y[1].Number * 255),
							Convert.ToInt32(y[2].Number * 255),
							255), gm);
				}
				catch
				{
					return DynValue.Void;
				}
			});
		}
		public static void MakeDataType(GameManager gm, string name, Func<ScriptExecutionContext, CallbackArguments, DynValue> func)
		{
			var it = new Table(gm.MainEnvironment);
			it["new"] = DynValue.NewCallback(func);
			gm.MainEnvironment.Globals[name] = DynValue.NewTable(it);
		}
		public static Table ShallowCloneTable(Table t)
		{
			Table ta = new Table(t.OwnerScript);
			t.Pairs.ToList().ForEach(x => ta.Set(x.Key, x.Value));
			return ta;
		}
		public static void PrintOut(string msg)
		{
			LogManager.LogInfo(msg);
		}
		public static void PrintWarn(string msg)
		{
			LogManager.LogWarn(msg);
		}
		public static void PrintError(string msg)
		{
			LogManager.LogError(msg);
		}
		public static Table MakeInstanceTable(Instance? inst, GameManager gm)
		{
			var scr = gm.MainEnvironment;

			if (inst == null)
				return new Table(scr);

			// i want to bulge out my eyes
			if (inst.Tables.TryGetValue(scr, out Table? t)) return t;
			var type = inst.GetType();

			var table = new Table(scr)
			{
				MetaTable = new Table(scr)
			};

			var props = (IEnumerable<PropertyInfo?>)type.GetProperties();
			var meths = (IEnumerable<MethodInfo?>)type.GetMethods();

			props = from x in props where x.GetCustomAttribute<LuaAttribute>() != null select x;
			meths = from x in meths where x.GetCustomAttribute<LuaAttribute>() != null select x;

			table.MetaTable["__index"] = DynValue.NewCallback((x, y) =>
			{
				var key = y[1].String;
				var prop = (from z in props where z.Name == key select z).FirstOrDefault();
				var meth = (from z in meths where z.Name == key select z).FirstOrDefault();

				if (prop != null)
				{
					if (prop.GetValue(inst) == null)
						return DynValue.Nil;

					if (!SerializationManager.LuaSerializers.TryGetValue(prop.PropertyType.FullName!, out var ls))
						return DynValue.Nil;

					var val = prop.GetValue(inst);

					if (val != null)
						return ls(val, gm);
					else
						return DynValue.Nil;
				}
				else
				{
					if (meth != null)
					{
						var sec = meth.GetCustomAttribute<LuaAttribute>();

						if (TaskScheduler.CurrentJob.Type != JobType.Script)
							throw new Exception("Attempted to access an Instance as a table from native code");

						if (sec == null) // we lie
							throw new ScriptRuntimeException($"\"{inst.GetType().Name}\" doesn't have a property, method or a child named \"{key}\"");

						if (Security.IsCompatible((int)TaskScheduler.CurrentJob.AssociatedObject1!, sec.Capabilities))
							return DynValue.NewCallback((a, b) =>
							{
								try
								{
									var args = new List<object?>();
									var parms = meth.GetParameters();

									for (int i = 0; i < parms.Length; i++)
									{
										var info = parms[i];
										var t = info.ParameterType;

										if (t != dvt)
										{
											if (!SerializationManager.LuaDeserializers.TryGetValue(t.FullName ?? "", out var ld))
												return DynValue.Nil;

											if (b[i + 1] == DynValue.Nil)
												args.Add(null);
											else
												args.Add(ld(b[i + 1], gm));
										}
										else
											args.Add(b[i + 1]);
									}

									var ret = meth!.Invoke(inst, args.ToArray());
									var rett = meth.ReturnType;

									if (meth.ReturnType.Name == "LuaYield")
									{
										var hasr = (bool)meth.ReturnType.GetField("HasResult")!.GetValue(ret)!; // idc lol
										if (!hasr)
											return DynValue.NewYieldReq(y.GetArray());
										else
											rett = meth.ReturnType.GetGenericArguments()[0];
									}
									if (!rett.IsArray)
									{
										if (!SerializationManager.LuaSerializers.TryGetValue(rett.FullName ?? "", out var ls))
											return DynValue.Nil;

										if (ret != null)
											return ls(ret, gm);
										else
											return DynValue.Nil;
									}
									else
									{
										Table res = new(scr);
										Array arr = (ret as Array)!;
										Type? elt = rett.GetElementType();

										if (elt == null) return DynValue.Nil;

										if (SerializationManager.LuaSerializers.TryGetValue(elt.FullName ?? "", out var ls))
										{
											for (int i = 0; i < arr.Length; i++)
											{
												var val = ls(arr.GetValue(i)!, gm); // i hate you vs debugger for fuck sake help
												res[i] = val;
											}
										}

										return DynValue.NewTable(res);
									}
								}
								catch (TargetInvocationException)
								{
									throw new ScriptRuntimeException($"\"{meth.Name}\" doesn't accept one or more of parameters provided to it");
								}
							});
						else
							throw new Exception($"\"{meth.Name}\" is not accessible");
					}
					else
					{
						Instance[] children = inst.GetChildren();
						Instance? child = (from h in children where h.Name == key select h).FirstOrDefault();

						if (prop == null && meth == null && child == null)
							throw new ScriptRuntimeException($"\"{inst.GetType().Name}\" doesn't have a property, method or a child named \"{key}\"");
						if (child == null)
							throw new ScriptRuntimeException($"\"{inst.GetType().Name}\" doesn't have a property, method or a child named \"{key}\""); // so vs COULD STFU

						return DynValue.NewTable(MakeInstanceTable(child, gm));
					}
				}
			});
			table.MetaTable["__newindex"] = DynValue.NewCallback((x, y) =>
			{
				var key = y[1].String;
				var val = y[2];
				var prop = (from z in props where z.Name == key select z).FirstOrDefault();

				if (prop == null)
					throw new ScriptRuntimeException($"\"{type.Name}\" doesn't have a property named \"{key}\"");
				else
				{
					if (prop.CanWrite)
					{
						if (val == DynValue.Nil)
							prop.SetValue(inst!, null);
						else
						{
							if (prop.Name == "Parent")
							{
								if (val.Type != DataType.Table)
									throw new ScriptRuntimeException($"Property \"Parent\" of \"{type.Name}\" only accepts Instance");
								if (val.Table.MetaTable == null)
									throw new ScriptRuntimeException($"Property \"Parent\" of \"{type.Name}\" only accepts Instance");
								if (val.Table.MetaTable["__handle"] == null)
									throw new ScriptRuntimeException($"Property \"Parent\" of \"{type.Name}\" only accepts Instance");

								if (Guid.TryParse(val.Table.MetaTable["__handle"].ToString(), out Guid uid))
								{
									prop.SetValue(inst!, gm.AllInstances.Find(x => x.UniqueID == uid));
									if (gm.NetworkManager.IsServer)
										gm.NetworkManager.AddReplication(inst, NetworkManager.Replication.REPM_BUTOWNER, NetworkManager.Replication.REPW_REPARNT, false);
								}
								else
									throw new ScriptRuntimeException($"Attempted to assign Instance's parent to foreign Instance");
							}
							else
							{
								if (!SerializationManager.LuaDeserializers.TryGetValue(prop.PropertyType.FullName ?? "", out var ld))
									return DynValue.Nil;
								else
								{
									var ret = ld(val, gm);
									var exc = SerializationManager.LuaDataTypes[prop.PropertyType.FullName ?? ""];

									if (val.Type != exc)
										throw new ScriptRuntimeException($"Property \"{key}\" of \"{type.Name}\" only accepts {exc}");

									prop.SetValue(inst!, ret);
									if (gm.NetworkManager.IsServer)
										gm.NetworkManager.AddReplication(inst, NetworkManager.Replication.REPM_TOALL, NetworkManager.Replication.REPW_PROPCHG, false);
								}
							}
						}
					}
					else
						throw new ScriptRuntimeException($"Property \"{key}\" of \"{type.Name}\" is read-only");
				}
				return DynValue.Nil;
			});
			table.MetaTable["__tostring"] = DynValue.NewCallback((x, y) => DynValue.NewString(inst.Name));
			table.MetaTable["__handle"] = inst.UniqueID.ToString();
			table.MetaTable["__handleType"] = 0;

			inst.Tables[scr] = table;

			return table;
		}
	}
}
