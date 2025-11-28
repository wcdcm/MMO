using System;

namespace Summer
{
    public class Singleton<T> where T : new()
    {
        private static T? m_Instance;

        // 静态锁对象，用于同步操作
        private static readonly object _lock = new object();

        public static T Instance
        {
            get
            {
                // 第一次检查，避免每次访问都加锁
                if (m_Instance == null)
                {
                    // 加锁，确保同一时间只有一个线程进入临界区
                    lock (_lock)
                    {
                        // 第二次检查，防止多个线程等待锁时重复创建实例
                        if (m_Instance == null)
                        {
                            m_Instance = new T();
                        }
                    }
                }
                return m_Instance;
            }
        }
    }
}
