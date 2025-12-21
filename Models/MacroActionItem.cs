using System;

namespace MacroApp.Models
{
    /// <summary>
    /// عنصر إجراء في تسلسل الماكرو
    /// </summary>
    public class MacroActionItem
    {
        /// <summary>
        /// نوع الإجراء
        /// </summary>
        public MacroActionType ActionType { get; set; }

        /// <summary>
        /// اسم الإجراء للعرض
        /// </summary>
        public string ActionName { get; set; }

        /// <summary>
        /// مسار الأيقونة
        /// </summary>
        public string IconPath { get; set; }

        /// <summary>
        /// مدة الإجراء بالملي ثانية
        /// </summary>
        public int Duration { get; set; }

        /// <summary>
        /// نص المدة للعرض
        /// </summary>
        public string DurationText => $"{Duration}ms";

        /// <summary>
        /// زر الماوس (للإجراءات المتعلقة بالماوس)
        /// </summary>
        public MouseButton MouseButton { get; set; }

        /// <summary>
        /// معرف فريد للعنصر
        /// </summary>
        public Guid Id { get; set; } = Guid.NewGuid();

        public MacroActionItem()
        {
        }

        public MacroActionItem(MacroActionType actionType, MouseButton mouseButton, int duration)
        {
            ActionType = actionType;
            MouseButton = mouseButton;
            Duration = duration;
            UpdateDisplayProperties();
        }

        /// <summary>
        /// تحديث خصائص العرض بناءً على نوع الإجراء
        /// </summary>
        public void UpdateDisplayProperties()
        {
            switch (ActionType)
            {
                case MacroActionType.MouseDown:
                    ActionName = GetMouseButtonName() + " Down";
                    IconPath = GetMouseDownIcon();
                    break;

                case MacroActionType.MouseUp:
                    ActionName = GetMouseButtonName() + " Up";
                    IconPath = GetMouseUpIcon();
                    break;

                case MacroActionType.Delay:
                    ActionName = "تأخير";
                    IconPath = "DelayIcon";
                    break;
            }
        }

        private string GetMouseButtonName()
        {
            return MouseButton switch
            {
                MouseButton.Left => "يسار",
                MouseButton.Right => "يمين",
                MouseButton.Middle => "أوسط",
                MouseButton.X1 => "رابع",
                MouseButton.X2 => "خامس",
                _ => "غير معروف"
            };
        }

        private string GetMouseDownIcon()
        {
            return MouseButton switch
            {
                MouseButton.Left => "MouseLeftDownIcon",
                MouseButton.Right => "MouseRightDownIcon",
                MouseButton.Middle => "MouseLeftDownIcon", // استخدام نفس أيقونة اليسار مؤقتاً
                MouseButton.X1 => "MouseLeftDownIcon",
                MouseButton.X2 => "MouseRightDownIcon",
                _ => "MouseLeftDownIcon"
            };
        }

        private string GetMouseUpIcon()
        {
            return MouseButton switch
            {
                MouseButton.Left => "MouseLeftUpIcon",
                MouseButton.Right => "MouseRightUpIcon",
                MouseButton.Middle => "MouseLeftUpIcon", // استخدام نفس أيقونة اليسار مؤقتاً
                MouseButton.X1 => "MouseLeftUpIcon",
                MouseButton.X2 => "MouseRightUpIcon",
                _ => "MouseLeftUpIcon"
            };
        }
    }

    /// <summary>
    /// أنواع إجراءات الماكرو
    /// </summary>
    public enum MacroActionType
    {
        MouseDown,
        MouseUp,
        Delay
    }

    /// <summary>
    /// أزرار الماوس
    /// </summary>
    public enum MouseButton
    {
        Left,
        Right,
        Middle,
        X1,
        X2
    }
}