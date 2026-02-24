/*---------------------------------
 *Title:UI自动化组件生成代码生成工具
 *Author:铸梦
 *Date:2026/2/24 11:22:41
 *Description:变量需要以[Text]括号加组件类型的格式进行声明，然后右键窗口物体—— 一键生成UI数据组件脚本即可
 *注意:以下文件是自动生成的，任何手动修改都会被下次生成覆盖,若手动修改后,尽量避免自动生成
---------------------------------*/
using UnityEngine.UI;
using UnityEngine;

namespace ZMUIFrameWork
{
	public class SettingWindowDataComponent:MonoBehaviour
	{
		public   Toggle  AudioToggle;

		public   Toggle  OtherToggle;

		public   Transform  AudioSettingTransform;

		public   Slider  AudioVolumeSlider;

		public   Slider  MusicVolumeSlider;

		public   Slider  SoundVolumeSlider;

		public   Transform  OtherSettingTransform;

		public   Button  CloseButton;

		public  void InitComponent(WindowBase target)
		{
		     //组件事件绑定
		     SettingWindow mWindow=(SettingWindow)target;
		     target.AddToggleClickListener(AudioToggle,mWindow.OnAudioToggleChange);
		     target.AddToggleClickListener(OtherToggle,mWindow.OnOtherToggleChange);
		     target.AddButtonClickListener(CloseButton,mWindow.OnCloseButtonClick);
		}
	}
}
