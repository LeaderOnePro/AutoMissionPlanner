using System;
using System.Collections.Generic;

namespace AutoMissionPlanner.VLAControl
{
    public class CommandParser
    {
        // 指令到动作的映射
        private readonly Dictionary<string, Func<string, ActionCommand>> commandMap;
        
        public CommandParser()
        {
            commandMap = new Dictionary<string, Func<string, ActionCommand>>
            {
                ["TAKEOFF"] = param => new ActionCommand
                {
                    Type = ActionType.Takeoff,
                    Parameters = new Dictionary<string, object>
                    {
                        ["altitude"] = param != string.Empty ? float.Parse(param) : 5.0f // 默认高度5米
                    }
                },
                
                ["LAND"] = _ => new ActionCommand
                {
                    Type = ActionType.Land,
                    Parameters = new Dictionary<string, object>()
                },
                
                ["RTL"] = _ => new ActionCommand
                {
                    Type = ActionType.ReturnToLaunch,
                    Parameters = new Dictionary<string, object>()
                },
                
                ["FORWARD"] = param => new ActionCommand
                {
                    Type = ActionType.Move,
                    Parameters = new Dictionary<string, object>
                    {
                        ["direction"] = "forward",
                        ["distance"] = param != string.Empty ? float.Parse(param) : 1.0f
                    }
                },
                
                ["BACKWARD"] = param => new ActionCommand
                {
                    Type = ActionType.Move,
                    Parameters = new Dictionary<string, object>
                    {
                        ["direction"] = "backward",
                        ["distance"] = param != string.Empty ? float.Parse(param) : 1.0f
                    }
                },
                
                ["LEFT"] = param => new ActionCommand
                {
                    Type = ActionType.Move,
                    Parameters = new Dictionary<string, object>
                    {
                        ["direction"] = "left",
                        ["distance"] = param != string.Empty ? float.Parse(param) : 1.0f
                    }
                },
                
                ["RIGHT"] = param => new ActionCommand
                {
                    Type = ActionType.Move,
                    Parameters = new Dictionary<string, object>
                    {
                        ["direction"] = "right",
                        ["distance"] = param != string.Empty ? float.Parse(param) : 1.0f
                    }
                },
                
                ["LOITER"] = param => new ActionCommand
                {
                    Type = ActionType.Loiter,
                    Parameters = new Dictionary<string, object>
                    {
                        ["time"] = param != string.Empty ? int.Parse(param) : 10 // 默认10秒
                    }
                }
            };
        }
        
        public ActionCommand ParseCommand(string modelOutput)
        {
            if (string.IsNullOrEmpty(modelOutput))
                return null;
                
            // 解析命令格式: COMMAND_NAME:PARAMETER
            string[] parts = modelOutput.Split(':');
            string commandName = parts[0].Trim().ToUpper();
            string parameter = parts.Length > 1 ? parts[1].Trim() : string.Empty;
            
            if (commandMap.TryGetValue(commandName, out var commandFactory))
            {
                try
                {
                    return commandFactory(parameter);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"解析命令参数失败: {ex.Message}");
                    return null;
                }
            }
            
            return new ActionCommand
            {
                Type = ActionType.Unknown,
                Parameters = new Dictionary<string, object>
                {
                    ["original"] = modelOutput
                }
            };
        }
    }
    
    public enum ActionType
    {
        Unknown,
        Takeoff,
        Land,
        Move,
        ReturnToLaunch,
        Loiter
    }
    
    public class ActionCommand
    {
        public ActionType Type { get; set; }
        public Dictionary<string, object> Parameters { get; set; }
    }
}
