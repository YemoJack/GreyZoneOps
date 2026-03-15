using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Text.RegularExpressions;

public class UIWindowEditor : EditorWindow
{
    private string scriptContent;
    private string filePath;
    private Vector2 scroll = new Vector2();
    private Dictionary<string, string> mMethodDic = new Dictionary<string, string>();

    /// <summary>
    /// 显示代码展示窗口
    /// </summary>
    public static void ShowWindow(string content, string filePath, Dictionary<string, string> insterDic = null)
    {
        //创建代码展示窗口
        UIWindowEditor window = (UIWindowEditor)GetWindowWithRect(typeof(UIWindowEditor), new Rect(100, 50, 800, 700), false, "Window生成界面");
        window.scriptContent = BuildScriptContent(content, filePath, insterDic);
        window.filePath = filePath;
        //处理代码新增
        window.mMethodDic = insterDic;
        window.Show();
    }

    public static string BuildScriptContent(string content, string filePath, Dictionary<string, string> insterDic = null)
    {
        if (File.Exists(filePath) == false || insterDic == null)
        {
            return content;
        }

        string originScript = File.ReadAllText(filePath);
        if (string.IsNullOrEmpty(originScript))
        {
            return content;
        }

        bool isInsterSuccess = false;
        UIWindowEditor helper = CreateInstance<UIWindowEditor>();
        foreach (var item in insterDic)
        {
            if (originScript.Contains(item.Key))
            {
                continue;
            }

            int insterIndex = helper.GetInserIndex(originScript);
            if (insterIndex < 0)
            {
                insterIndex = helper.GetClassInsertIndex(originScript);
                if (insterIndex < 0)
                {
                    continue;
                }
            }

            //插入新增的数据
            originScript = originScript.Insert(insterIndex, item.Value + "\t\t");
            isInsterSuccess = true;
        }

        DestroyImmediate(helper);
        return isInsterSuccess ? originScript : originScript;
    }

    public static void SaveScript(string content, string filePath, Dictionary<string, string> insterDic = null)
    {
        string script = BuildScriptContent(content, filePath, insterDic);
        string directory = Path.GetDirectoryName(filePath);
        if (string.IsNullOrEmpty(directory) == false && Directory.Exists(directory) == false)
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(filePath, script);
    }

    public void OnGUI()
    {
        //绘制ScroView
        scroll = EditorGUILayout.BeginScrollView(scroll,GUILayout.Height(600),GUILayout.Width(800));
        EditorGUILayout.TextArea(scriptContent);
        EditorGUILayout.EndScrollView();
        EditorGUILayout.Space();

        //绘制脚本生成路径
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.TextArea("脚本生成路径："+filePath);
        if (GUILayout.Button("选择路径",GUILayout.Width(80)))
        {
            filePath= EditorUtility.OpenFolderPanel("脚本生成路径", filePath, "ZMUI");
        }
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.Space();

        //绘制模块按钮
        //EditorGUILayout.BeginHorizontal();
        //for (int i = 0; i < UISetting.Instance.windowGenerateDataList.Count; i++)
        //{
        //    WindowGenerateData data= UISetting.Instance.windowGenerateDataList[i];
        //    data.isSelect= GUILayout.Toggle(data.isSelect, data.moduleName);
        //    if (data.isSelect)
        //    {

        //    }
        //}
        //EditorGUILayout.EndHorizontal();

        //绘制按钮
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("生成脚本",GUILayout.Height(30)))
        {
            //按钮事件
            ButtonClick();
        }
        EditorGUILayout.EndHorizontal();

    }
    public void ButtonClick()
    {
        SaveScript(scriptContent, filePath);
        mMethodDic = null;
        scriptContent = string.Empty;
         
        Debug.Log("Create Code finish! Cs path:" + filePath);
        AssetDatabase.Refresh();
        if (EditorUtility.DisplayDialog("自动化工具", "生成脚本成功！", "确定"))
        {
            Close();
        } 
     }
    /// <summary>
    /// 获取插入代码的下标
    /// </summary>
    /// <param name="content"></param>
    /// <returns></returns>
    public int GetInserIndex(string content)
    {
        //找到UI事件组件下面的第一个public 所在的位置 进行插入
        Regex regex = new Regex("UI组件事件");
        Match match = regex.Match(content);
        if (match.Success == false)
        {
            return -1;
        }

        Regex regex1 = new Regex("public");
        MatchCollection matchColltion = regex1.Matches(content);

        for (int i = 0; i < matchColltion.Count; i++)
        {
            if (matchColltion[i].Index > match.Index)
            {
                //Debug.Log(matchColltion[i].Index);
                return matchColltion[i].Index;
            }
        }
        return -1;

    }

    public int GetClassInsertIndex(string content)
    {
        if (string.IsNullOrEmpty(content))
        {
            return -1;
        }

        return content.LastIndexOf("}");
    }
}
