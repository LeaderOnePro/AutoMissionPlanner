using System;
using System.Windows.Forms;
using MissionPlanner;
using MissionPlanner.Plugin;

namespace AutoMissionPlanner.VLAControl
{
    public class VLAControlPlugin : Plugin
    {
        public override string Name => "VLA控制系统";
        public override string Version => "1.0";
        public override string Author => "AutoMissionPlanner";
        
        private VLAControlUI controlUI;
        private VLAModel vlaModel;
        private ImageProcessor imageProcessor;
        private CommandParser commandParser;
        private ActionExecutor actionExecutor;

        public override bool Init()
        {
            try
            {
                vlaModel = new VLAModel();
                imageProcessor = new ImageProcessor();
                commandParser = new CommandParser();
                actionExecutor = new ActionExecutor();
                
                // 初始化VLA模型
                if (!vlaModel.Initialize())
                {
                    MessageBox.Show("VLA模型初始化失败", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return false;
                }
                
                // 添加菜单项
                ToolStripMenuItem menu = new ToolStripMenuItem("VLA控制");
                menu.Click += (s, e) => ShowUI();
                
                Host.MainForm.MainMenu.Items.Add(menu);
                
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"插件初始化失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }
        
        private void ShowUI()
        {
            if (controlUI == null || controlUI.IsDisposed)
            {
                controlUI = new VLAControlUI(vlaModel, imageProcessor, commandParser, actionExecutor);
            }
            
            controlUI.Show();
            controlUI.BringToFront();
        }

        public override bool Loaded()
        {
            return true;
        }

        public override bool Exit()
        {
            vlaModel.Dispose();
            return true;
        }
    }
}
