//-----------------------------------------------------------------------
// <copyright file="Program.cs" company="Bruce">
//     Copyright (c) Bruce. All rights reserved.
// </copyright>
// <author>Bruce He</author>
//-----------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ImoocConsoleApp
{
    /// <summary>
    /// Default class in .net 
    /// </summary>
    public static class Program
    {
        /// <summary>
        /// Mark is all thread end and stop this application
        /// </summary>
        public static bool isStop = false;
        
        /// <summary>
        /// Initializes static members of the <see cref="Program" /> class.
        /// </summary>
        static Program()
        {
            //这个绑定事件必须要在引用到ConsoleApplication1这个程序集的方法之前,注意是方法之前,不是语句之间,就算语句是在方法最后一行,在进入方法的时候就会加载程序集,如果这个时候没有绑定事件,则直接抛出异常,或者程序终止了
            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
        }

        /// <summary>
        /// 由于程序解析Json文档时会使用Newtonsoft.Json.dll
        /// 但是在控制台程序发布时必须随身携带此DLL
        /// 所以为了方便讲DLL打包进入Exe中便有了以下方法
        /// 使用时将DLL作为资源放入项目的解决方案中并在属性栏中将其生成属性设置为‘嵌入的资源’（注意不是只添加引用）
        /// 此程序会在引用找不到时在自身的EXE中反编译出Newtonsoft.Json.dll保证程序正常运行
        /// </summary>
        /// <param name="sender">事件的触发者</param>
        /// <param name="args">参数</param>
        /// <returns>反射出的Newtonsoft对象</returns>
        static System.Reflection.Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            //获取加载失败的程序集的全名
            var assName = new AssemblyName(args.Name).FullName;
            if (args.Name == "Newtonsoft.Json, Version=4.5.0.0, Culture=neutral, PublicKeyToken=30ad4fe6b2a6aeed")
            {
                //读取资源
                using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("ImoocConsoleApp.Newtonsoft.Json.dll"))
                {
                    var bytes = new byte[stream.Length];
                    stream.Read(bytes, 0, (int)stream.Length);

                    //加载资源文件中的dll,代替加载失败的程序集
                    return Assembly.Load(bytes);
                }
            }
            throw new DllNotFoundException(assName);
        }

        /// <summary>
        /// 程序的入口方法
        /// </summary>
        /// <param name="args">参数</param>
        static void Main(string[] args)
        {
            imoocV2.Run();
            while (!isStop)
            {
                Thread.Sleep(1000);
            }
            Console.WriteLine("press any key to exit...");
            Console.ReadKey();
        }
    }
}
