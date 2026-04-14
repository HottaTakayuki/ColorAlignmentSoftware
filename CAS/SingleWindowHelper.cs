using System;
using System.Collections.Generic;
using System.Windows;

namespace CAS
{
    /// <summary>
    /// 単一のWindow表示を行うためのHelper
    /// </summary>
    public static class SingleWindowHelper
    {
        private static readonly Dictionary<Type, Window> _instances = new Dictionary<Type, Window>();

        /// <summary>
        /// Window表示を行う
        /// 既に表示済の場合はアクティブ化のみ行う
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="factory">Window生成処理、コンストラクタ</param>
        /// <param name="owner">WindowのOwner、基本はMainWindow</param>
        public static void Show<T>(Func<T> factory, Window owner) where T : Window
        {
            if (_instances.TryGetValue(typeof(T), out var w) && w.IsVisible)
            {
                if (w.WindowState == WindowState.Minimized)
                {
                    w.WindowState = WindowState.Normal;
                }

                w.Activate();
                return;
            }

            var window = factory();
            window.Closed += (_, __) => _instances.Remove(typeof(T));
            _instances[typeof(T)] = window;
            window.Owner = owner;
            window.Show();
        }

        /// <summary>
        /// Windowを閉じる
        /// </summary>
        /// <typeparam name="T"></typeparam>
        public static void Close<T>() where T : Window
        {
            if (_instances.TryGetValue(typeof(T), out var w))
            {
                w.Close();
            }
        }
    }
}
