using UnityEngine;

namespace LightInDark.Roles
{
    /// <summary>职业文档接口：为帮助菜单与开场白提供展示数据</summary>
    public interface IRoleDocument
    {
        /// <summary>职业立绘（帮助详情左上角），为 null 时不显示</summary>
        Sprite IconImage { get; }

        /// <summary>开场白（分配职业时显示在职业名底部），为空时不显示</summary>
        string IntroBlurb { get; }

        /// <summary>技能介绍（帮助详情立绘下方），多行用 \n 分隔</summary>
        string SkillDescription { get; }
    }
}
