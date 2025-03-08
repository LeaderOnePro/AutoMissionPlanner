using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MissionPlanner;
using MissionPlanner.Utilities;

namespace AutoMissionPlanner.VLAControl
{
    public class ActionExecutor
    {
        public event EventHandler<string> ActionStatusChanged;
        
        public async Task<bool> ExecuteCommand(ActionCommand command)
        {
            if (command == null)
            {
                RaiseStatusChange("无效命令");
                return false;
            }
            
            // 检查是否已连接到无人机
            if (MainV2.comPort == null || !MainV2.comPort.BaseStream.IsOpen)
            {
                RaiseStatusChange("未连接到无人机");
                return false;
            }
            
            try
            {
                switch (command.Type)
                {
                    case ActionType.Takeoff:
                        return await ExecuteTakeoff(command);
                        
                    case ActionType.Land:
                        return await ExecuteLand(command);
                        
                    case ActionType.ReturnToLaunch:
                        return await ExecuteRTL(command);
                        
                    case ActionType.Move:
                        return await ExecuteMove(command);
                        
                    case ActionType.Loiter:
                        return await ExecuteLoiter(command);
                        
                    case ActionType.Unknown:
                    default:
                        RaiseStatusChange($"未知命令类型: {command.Type}");
                        return false;
                }
            }
            catch (Exception ex)
            {
                RaiseStatusChange($"执行命令失败: {ex.Message}");
                return false;
            }
        }
        
        private async Task<bool> ExecuteTakeoff(ActionCommand command)
        {
            float altitude = (float)command.Parameters["altitude"];
            RaiseStatusChange($"正在起飞到高度 {altitude}m");
            
            try
            {
                MainV2.comPort.setMode("GUIDED");
                await Task.Delay(500);
                
                MainV2.comPort.doCommand(MAVLink.MAV_CMD.TAKEOFF, 0, 0, 0, 0, 0, 0, altitude);
                return true;
            }
            catch (Exception ex)
            {
                RaiseStatusChange($"起飞失败: {ex.Message}");
                return false;
            }
        }
        
        private async Task<bool> ExecuteLand(ActionCommand command)
        {
            RaiseStatusChange("正在执行降落");
            
            try
            {
                MainV2.comPort.setMode("LAND");
                return true;
            }
            catch (Exception ex)
            {
                RaiseStatusChange($"降落失败: {ex.Message}");
                return false;
            }
        }
        
        private async Task<bool> ExecuteRTL(ActionCommand command)
        {
            RaiseStatusChange("正在返航");
            
            try
            {
                MainV2.comPort.setMode("RTL");
                return true;
            }
            catch (Exception ex)
            {
                RaiseStatusChange($"返航失败: {ex.Message}");
                return false;
            }
        }
        
        private async Task<bool> ExecuteMove(ActionCommand command)
        {
            string direction = (string)command.Parameters["direction"];
            float distance = (float)command.Parameters["distance"];
            
            RaiseStatusChange($"正在向{direction}移动{distance}米");
            
            try
            {
                // 确保处于GUIDED模式
                MainV2.comPort.setMode("GUIDED");
                await Task.Delay(500);
                
                // 获取当前位置
                float lat = (float)MainV2.comPort.MAV.cs.lat;
                float lng = (float)MainV2.comPort.MAV.cs.lng;
                float alt = (float)MainV2.comPort.MAV.cs.alt;
                
                // 计算新位置
                PointLatLngAlt newPos = CalculateNewPosition(lat, lng, alt, direction, distance);
                
                // 移动到新位置
                MainV2.comPort.setGuidedModeWP(new Locationwp
                {
                    lat = newPos.Lat,
                    lng = newPos.Lng,
                    alt = (float)newPos.Alt
                });
                
                return true;
            }
            catch (Exception ex)
            {
                RaiseStatusChange($"移动失败: {ex.Message}");
                return false;
            }
        }
        
        private PointLatLngAlt CalculateNewPosition(float lat, float lng, float alt, string direction, float distance)
        {
            double bearing = 0;
            
            switch (direction.ToLower())
            {
                case "forward":
                    bearing = MainV2.comPort.MAV.cs.yaw;
                    break;
                case "backward":
                    bearing = (MainV2.comPort.MAV.cs.yaw + 180) % 360;
                    break;
                case "left":
                    bearing = (MainV2.comPort.MAV.cs.yaw - 90) % 360;
                    break;
                case "right":
                    bearing = (MainV2.comPort.MAV.cs.yaw + 90) % 360;
                    break;
            }
            
            return new PointLatLngAlt(lat, lng).newpos(bearing, distance);
        }
        
        private async Task<bool> ExecuteLoiter(ActionCommand command)
        {
            int time = (int)command.Parameters["time"];
            RaiseStatusChange($"正在原地盘旋{time}秒");
            
            try
            {
                MainV2.comPort.setMode("LOITER");
                await Task.Delay(time * 1000);
                return true;
            }
            catch (Exception ex)
            {
                RaiseStatusChange($"盘旋失败: {ex.Message}");
                return false;
            }
        }
        
        private void RaiseStatusChange(string status)
        {
            ActionStatusChanged?.Invoke(this, status);
        }
    }
}
