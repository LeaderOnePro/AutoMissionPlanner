using System;
using System.Drawing;
using System.Drawing.Imaging;
using MissionPlanner;
using MissionPlanner.Utilities;

namespace AutoMissionPlanner.VLAControl
{
    public class ImageProcessor
    {
        private Bitmap lastFrame;
        private readonly object frameLock = new object();
        private bool isCapturing = false;
        
        public event EventHandler<Bitmap> FrameReceived;

        public void StartCapture()
        {
            if (isCapturing)
                return;
            
            isCapturing = true;
            
            // 订阅MissionPlanner视频流事件
            try
            {
                if (MainV2.comPort.MAV != null && MainV2.comPort.MAV.cs != null)
                {
                    MainV2.comPort.MAV.cs.UpdateCameraImage += OnNewFrame;
                    Console.WriteLine("已连接到视频流");
                }
                else
                {
                    Console.WriteLine("无法连接到视频流：MAV未初始化");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"启动视频捕获失败: {ex.Message}");
            }
        }
        
        public void StopCapture()
        {
            if (!isCapturing)
                return;
            
            isCapturing = false;
            
            // 取消订阅事件
            try
            {
                if (MainV2.comPort.MAV != null && MainV2.comPort.MAV.cs != null)
                {
                    MainV2.comPort.MAV.cs.UpdateCameraImage -= OnNewFrame;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"停止视频捕获失败: {ex.Message}");
            }
        }
        
        private void OnNewFrame(object sender, EventArgs e)
        {
            try
            {
                // 从MissionPlanner获取当前帧
                if (MainV2.comPort.MAV.cs.camimage != null)
                {
                    lock (frameLock)
                    {
                        // 复制一份图像以防止跨线程访问问题
                        lastFrame?.Dispose();
                        lastFrame = new Bitmap(MainV2.comPort.MAV.cs.camimage);
                    }
                    
                    // 触发帧接收事件
                    FrameReceived?.Invoke(this, lastFrame);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"处理视频帧时出错: {ex.Message}");
            }
        }
        
        public Bitmap GetCurrentFrame()
        {
            lock (frameLock)
            {
                return lastFrame != null ? new Bitmap(lastFrame) : null;
            }
        }
        
        public Bitmap ProcessImageForModel(Bitmap input)
        {
            if (input == null)
                return null;
                
            // 在这里进行图像预处理，例如调整大小、归一化等
            // 为了适应VLA模型的输入要求
            
            Bitmap processed = new Bitmap(224, 224);
            using (Graphics g = Graphics.FromImage(processed))
            {
                g.DrawImage(input, 0, 0, 224, 224);
            }
            
            return processed;
        }
    }
}
