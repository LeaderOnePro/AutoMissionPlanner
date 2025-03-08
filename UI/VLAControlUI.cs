using System;
using System.Drawing;
using System.Windows.Forms;
using System.Threading.Tasks;
using AutoMissionPlanner.VLAControl;

namespace AutoMissionPlanner.VLAControl
{
    public partial class VLAControlUI : Form
    {
        private readonly VLAModel vlaModel;
        private readonly ImageProcessor imageProcessor;
        private readonly CommandParser commandParser;
        private readonly ActionExecutor actionExecutor;
        
        private bool isProcessing = false;

        public VLAControlUI(VLAModel vlaModel, ImageProcessor imageProcessor, CommandParser commandParser, ActionExecutor actionExecutor)
        {
            InitializeComponent();
            
            this.vlaModel = vlaModel;
            this.imageProcessor = imageProcessor;
            this.commandParser = commandParser;
            this.actionExecutor = actionExecutor;
            
            // 注册事件
            imageProcessor.FrameReceived += OnFrameReceived;
            actionExecutor.ActionStatusChanged += OnActionStatusChanged;
            
            // 启动视频捕获
            imageProcessor.StartCapture();
        }
        
        private void OnFrameReceived(object sender, Bitmap frame)
        {
            if (videoPreview.InvokeRequired)
            {
                videoPreview.BeginInvoke(new Action(() => UpdateVideoPreview(frame)));
            }
            else
            {
                UpdateVideoPreview(frame);
            }
        }
        
        private void UpdateVideoPreview(Bitmap frame)
        {
            // 更新视频预览
            if (frame != null)
            {
                if (videoPreview.Image != null)
                {
                    videoPreview.Image.Dispose();
                }
                videoPreview.Image = new Bitmap(frame);
            }
        }
        
        private void OnActionStatusChanged(object sender, string status)
        {
            if (statusTextBox.InvokeRequired)
            {
                statusTextBox.BeginInvoke(new Action(() => UpdateStatus(status)));
            }
            else
            {
                UpdateStatus(status);
            }
        }
        
        private void UpdateStatus(string status)
        {
            statusTextBox.AppendText($"[{DateTime.Now:HH:mm:ss}] {status}" + Environment.NewLine);
            // 自动滚动到底部
            statusTextBox.SelectionStart = statusTextBox.Text.Length;
            statusTextBox.ScrollToCaret();
        }
        
        private async void btnProcessCommand_Click(object sender, EventArgs e)
        {
            if (isProcessing)
                return;
                
            string command = txtCommand.Text.Trim();
            if (string.IsNullOrEmpty(command))
            {
                UpdateStatus("请输入命令");
                return;
            }
            
            Bitmap currentFrame = imageProcessor.GetCurrentFrame();
            if (currentFrame == null)
            {
                UpdateStatus("无法获取视频帧");
                return;
            }
            
            try
            {
                isProcessing = true;
                btnProcessCommand.Enabled = false;
                UpdateStatus($"正在处理命令: {command}");
                
                // 处理图像
                Bitmap processedImage = imageProcessor.ProcessImageForModel(currentFrame);
                
                // 通过VLA模型处理输入
                string modelOutput = await vlaModel.ProcessInputAsync(processedImage, command);
                UpdateStatus($"模型输出: {modelOutput}");
                
                // 解析模型输出为具体动作
                ActionCommand actionCommand = commandParser.ParseCommand(modelOutput);
                
                if (actionCommand != null)
                {
                    // 执行动作
                    bool success = await actionExecutor.ExecuteCommand(actionCommand);
                    UpdateStatus(success ? "命令执行成功" : "命令执行失败");
                }
                else
                {
                    UpdateStatus("无法理解命令");
                }
                
                processedImage.Dispose();
            }
            catch (Exception ex)
            {
                UpdateStatus($"处理错误: {ex.Message}");
            }
            finally
            {
                isProcessing = false;
                btnProcessCommand.Enabled = true;
                txtCommand.Clear();
            }
        }
        
        private void VLAControlUI_FormClosing(object sender, FormClosingEventArgs e)
        {
            // 停止视频捕获
            imageProcessor.StopCapture();
            
            // 取消事件注册
            imageProcessor.FrameReceived -= OnFrameReceived;
            actionExecutor.ActionStatusChanged -= OnActionStatusChanged;
        }
    }
}
