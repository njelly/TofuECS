using System;

namespace Tofunaut.TofuECS
{
    public interface ILogService
    {
        void Info(string s);
        void Warn(string s);
        void Error(string s);
        void Exception(Exception e);
    }
}