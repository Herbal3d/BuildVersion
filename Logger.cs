/*
 * Copyright (c) 2022 Robert Adams
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;

namespace org.herbal3d.buildVersion {
    public enum LogLevels {
        Trace = 0,
        Debug,
        Information,
        Warning,
        Error
    }

    public interface BLogger {
        void SetLogLevel(LogLevels pLevel);
        void Trace(string pMsg, params Object[] pArgs);
        void Debug(string pMsg, params Object[] pArgs);
        void Info(string pMsg, params Object[] pArgs);
        void Warn(string pMsg, params Object[] pArgs);
        void Error(string pMsg, params Object[] pArgs);
    }

    internal class LoggerConsole : BLogger {

        private AppParams _params;
        public LoggerConsole(AppParams pParams) {
            _params = pParams;
        }

        public void Debug(string pMsg, params object[] pArgs) {
            if (_params.Verbose) {
                Console.WriteLine(pMsg, pArgs);
            }
        }

        public void Error(string pMsg, params object[] pArgs) {
            Console.WriteLine(pMsg, pArgs);
        }

        public void Info(string pMsg, params object[] pArgs) {
            if (!_params.Quiet) {
                Console.WriteLine(pMsg, pArgs);
            }
        }

        public void SetLogLevel(LogLevels pLevel) {
        }

        public void Trace(string pMsg, params object[] pArgs) {
            Console.WriteLine(pMsg, pArgs);
        }

        public void Warn(string pMsg, params object[] pArgs) {
            Console.WriteLine(pMsg, pArgs);
        }
    }
}
