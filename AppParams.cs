/*
 * Copyright (c) 2016 Robert Adams
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
/*
 * Some code covered by: Copyright (c) Contributors, http://opensimulator.org/
 */

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using System.Text;

namespace org.herbal3d.buildVersion {
    public class ConfigParam : Attribute {
        public ConfigParam(string name, Type valueType, string desc = "", string? alt = null) {
            this.name = name;
            this.valueType = valueType;
            this.desc = desc;
            this.alt = alt;
        }
        public string name;
        public Type valueType;
        public string desc;
        public string? alt;
    }

    public class AppParams {

        public AppParams() {
        }

        // ========== General Input and Output Parameters
        [ConfigParam(name: "NameSpace", valueType: typeof(string), desc: "Namespace is set in the output file", alt: "ns")]
        public string NameSpace = "BuildVersion";
        [ConfigParam(name: "Version", valueType: typeof(string), desc: "Version to set. Expected to be formatted as num.num.num", alt: "v")]
        public string Version = "";
        [ConfigParam(name: "VersionFile", valueType: typeof(string), desc: "Version file to write. Default is 'VersionInfo.cs'", alt: "f")]
        public string VersionFile = "VersionInfo.cs";
        [ConfigParam(name: "AssemblyInfoFile", valueType: typeof(string), desc: "If specified, update the version info in AssemblyInfo.cs", alt: "a")]
        public string? AssemblyFile = null;
        [ConfigParam(name: "GitDir", valueType: typeof(string), desc: "Git directory. Default is './.git'")]
        public string GitDir = "./.git";

        [ConfigParam(name: "BuildDate", valueType: typeof(string), desc: "Build date if need to be set. Format: 'YYYYMMDD'. Default is today")]
        public string? BuildDate = null;
        [ConfigParam(name: "LongVersion", valueType: typeof(string), desc: "Long version. Default is built")]
        public string? LongVersion = null;

        [ConfigParam(name: "Print", valueType: typeof(bool), desc: "Don't write version file but print version info", alt: "p")]
        public bool Print = false;

        [ConfigParam(name: "IncrementBuild", valueType: typeof(bool), desc: "Increment the build part of the version", alt: "ib")]
        public bool IncrementBuild = false;
        [ConfigParam(name: "WriteAppVersion", valueType: typeof(string), desc: "Write the final application version to this file")]
        public string? WriteAppVersion = null;

        // ========== Debugging
        [ConfigParam(name: "Quiet", valueType: typeof(bool), desc: "supress as much informational output as possible")]
        public bool Quiet = false;
        [ConfigParam(name: "Verbose", valueType: typeof(bool), desc: "enable DEBUG information logging")]
        public bool Verbose = false;

        // Find the parameter definition and return the config info and the field info
        // Returns 'true' of the parameter is found. False otherwise.
        public bool TryGetParameterInfo(string pName, out ConfigParam? pConfigParam, out FieldInfo? pFieldInfo) {
            var lName = pName.ToLower();
            foreach (FieldInfo fi in this.GetType().GetFields()) {
                foreach (Attribute attr in Attribute.GetCustomAttributes(fi)) {
                    ConfigParam? cp = attr as ConfigParam;
                    if (cp != null) {
                        if (cp.name.ToLower() == lName || (cp.alt != null && cp.alt == lName)) {
                            pConfigParam = cp;
                            pFieldInfo = fi;
                            return true;
                        }
                    }
                }
            }
            pConfigParam = null;
            pFieldInfo = null;
            return false;
        }

        // Return a string version of a particular parameter value
        public string GetParameterValue(string pName) {
            string ret = "";
            foreach (FieldInfo fi in this.GetType().GetFields()) {
                foreach (Attribute attr in Attribute.GetCustomAttributes(fi)) {
                    ConfigParam? cp = attr as ConfigParam;
                    if (cp != null) {
                        if (cp.name == pName) {
                            object? val = fi.GetValue(this);
                            if (val != null) {
                                ret = val.ToString();
                            }
                            break;
                        }
                    }
                }
                if (ret.Length != 0) {
                    break;
                }
            }
            return ret;
        }
        // Set a parameter value
        public bool SetParameterValue(string pName, string pVal) {
            var ret = false;
            foreach (FieldInfo fi in this.GetType().GetFields()) {
                foreach (Attribute attr in Attribute.GetCustomAttributes(fi)) {
                    ConfigParam? cp = attr as ConfigParam;
                    if (cp != null) {
                        if (cp.name == pName) {
                            fi.SetValue(this, ConvertToObj(cp.valueType, pVal));
                            ret = true;
                            break;
                        }
                    }
                }
                if (ret) {
                    break;
                }
            }
            return ret;
        }
        public object? ConvertToObj(Type pT, object? pVal) {
            object? ret = null;
            if (pVal != null) {
                if (pVal.GetType() == pT) {
                    ret = pVal;
                }
                else {
                    try {
                        //Handling Nullable types i.e, int?, double?, bool? .. etc
                        if (Nullable.GetUnderlyingType(pT) != null) {
                            ret = TypeDescriptor.GetConverter(pT).ConvertFrom(pVal);
                        }
                        else {
                            if (pVal.GetType().GetMethod("ConvertTo") != null) {
                                ret = pVal.GetType().GetMethod("ConvertTo").Invoke(pVal, new object[] { pT });
                            }
                            else {
                                ret = Convert.ChangeType(pVal, pT);
                            }
                        }
                    }
                    catch (Exception) {
                        ret = default;
                    }
                }
            }
            return ret;
        }

        // Return a list of all the parameters and their descriptions
        public Dictionary<string, string> ListParameters() {
            var ret = new Dictionary<string,string>();
            foreach (FieldInfo fi in this.GetType().GetFields()) {
                foreach (Attribute attr in Attribute.GetCustomAttributes(fi)) {
                    ConfigParam? cp = attr as ConfigParam;
                    if (cp != null) {
                        ret.Add(cp.name, cp.desc);
                    }
                }
            }
            return ret;
        }

        /// <summary>
        /// Given parameters from the command line, read the parameters and set values specified
        /// </summary>
        /// <param name="args">array of command line tokens</param>
        /// <param name="firstOpParameter">if specified, presume the first token in the parameter line
        /// is a special value that should be assigned to this keyword (usually "--firstparams"</param>
        /// <param name="multipleLastParameters">if specified, presume multiple specs at the end of the
        /// line are filenames and pack them together in a CSV string and set to this key
        /// (usually "--lastparams")</param>
        /// <returns>'true' if all the parameters were parsed</returns>
        /// <exception cref="ArgumentException">throws an error string if something was wrong</exception>
        public bool MergeCommandLine(string[] args, string? firstOpParameter = null, string? multipleLastParameters = null) {
            bool ret = true;    // start out assuming parsing worked

            bool firstOpFlag = !String.IsNullOrEmpty(firstOpParameter);
            bool multipleLast = !String.IsNullOrEmpty(multipleLastParameters);

            for (int ii = 0; ii < args.Length; ii++) {
                string para = args[ii];
                // is this a parameter?
                if (para[0] == '-') {
                    ii += AddCommandLineParameter(para, (ii==(args.Length-1)) ? null : args[ii + 1]);
                }
                else {
                    if (ii == 0 && firstOpFlag) {
                        // if the first thing is not a parameter, make like it's an op or something
                        ii += AddCommandLineParameter(firstOpParameter ?? "", args[ii + 1]);
                    }
                    else {
                        if (multipleLast) {
                            // Pack all remaining arguments into a comma-separated list as LAST_PARAM
                            StringBuilder multFiles = new StringBuilder();
                            for (int jj = ii; jj < args.Length; jj++) {
                                if (multFiles.Length != 0) {
                                    multFiles.Append(",");
                                }
                                multFiles.Append(args[jj]);
                            }
                            AddCommandLineParameter(multipleLastParameters ?? "", multFiles.ToString());

                            // Skip them all
                            ii = args.Length;
                        }
                        else {
                            throw new ArgumentException("Unknown parameter " + para);
                        }
                    }
                }
            }

            return ret;
        }

        // Store the value for the parameter.
        // If we accept the value as a good value for the parameter, return 1 else 0.
        // A 'good value' is one that does not start with '-' or is not after a boolean parameter.
        // Return the number of parameters to advance the parameter line. That means, return
        //    a zero of we didn't used the next parameter and a 1 if the next parameter
        //    was used as a value so don't consider it the next parameter.
        private int AddCommandLineParameter(string pParm, string? val) {
            // System.Console.WriteLine(String.Format("AddCommandLineParameter: parm={0}, val={1}", pParm, val));
            int ret = 1;    // start off assuming the next token is the value we're setting
            string parm = pParm.ToLower();
            // Strip leading hyphens
            while (parm[0] == '-') {
                parm = parm.Substring(1);
            }

            // If the boolean parameter starts with "no", turn it off rather than on.
            string positiveAssertion = "true";
            if (parm.Length > 2 && parm[0] == 'n' && parm[1] == 'o') {
                string maybeParm = parm.Substring(2);
                if (TryGetParameterInfo(maybeParm, out ConfigParam? bcp, out FieldInfo? bfi)) {
                    if (bcp != null && bcp.valueType == typeof(Boolean)) {
                        // The parameter without the 'no' exists and is a boolean
                        positiveAssertion = "false";
                        parm = maybeParm;
                    }
                }
            }

            // If the next token starts with a parameter mark, it's not really a value
            if (val == null) {
                ret = 0;    // the next token is not used here to set the value
            }
            else {
                if (val[0] == '-') {
                    // TODO: add logic to see if param is numeric and allow negative values
                    val = null; // don't use the next token as a value
                    ret = 0;    // the next token is not used here to set the value
                }
            }

            if (TryGetParameterInfo(parm, out ConfigParam? cp, out FieldInfo? fi)) {
                // If the parameter is a boolean type and the next value is not a parameter,
                //      don't try to take up the next value.
                // This handles boolean flags.
                // If there is a value next (val != null) and that value is not the
                //    values 'true' or 'false' or 't' or 'f', then ignore the next value
                //    as not belonging to this flag. THis allows (and the logic above)
                //    allows:
                //        "--flag --otherFlag ...",
                //        "--flag something ...",
                //        "--flag true --otherFlag ...",
                //        "--noflag --otherflag ...",
                //        etc
                if (cp != null && cp.valueType == typeof(Boolean)) {
                    if (val != null) {
                        string valL = val.ToLower();
                        if (valL != "true" && valL != "t" && valL != "false" && valL != "f") {
                            // The value is not associated with this boolean so ignore it
                            val = null; // don't use the val token
                            ret = 0;    // the next token is not used here to set the value
                        }
                    }
                    if (val == null) {
                        // If the value is assumed, use the value based on the optional 'no'
                        val = positiveAssertion;
                    }
                }
                // Set the named parameter to the passed value
                fi?.SetValue(this, ConvertToObj(cp.valueType, val));
            }
            else {
                throw new ArgumentException("Unknown parameter " + parm);
            }
            return ret;
        }
    }
}
