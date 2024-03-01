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
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace org.herbal3d.buildVersion
{
    class BuildVersion {

        // Notes on the new net6 templating changes
        // https://docs.microsoft.com/en-us/dotnet/core/tutorials/top-level-templates
        // This project has disabled implicit usings and nullable contexts.

        AppParams appParams;
        BLogger log;

        static void Main(string[] args) {
            var parms = GetParams(args);
            if (parms is not null) {
                var logger = new LoggerConsole(parms);
                BuildVersion app = new BuildVersion(parms, logger);
                app.Start();
            }
            else {
                System.Console.WriteLine("Failed processing parameters");
            }
        }

        public BuildVersion(AppParams pParams, BLogger pLog) {
            appParams = pParams;
            log = pLog;
        }

        public void Start() {
            log.Info("BuildVersion {0}. See https://github.com/Misterblue/BuildVersion", VersionInfo.longVersion);

            // Verify passed version number is in good format
            string[] versionParts = appParams.Version.Split('.');
            if (versionParts.Length != 3) {
                log.Error("Specified version number must be in form 'num.num.num'. Given version = {0}", appParams.Version);
                return;
            }

            OptionallyIncrementBuildNumber(ref versionParts);

            // Get the Git version of the current HEAD
            string? gitVersion = GetGitVersion();
            if (gitVersion is not null) {

                // Built date is assumed to be when BuildVersion is run
                if (appParams.BuildDate == null) {
                    appParams.BuildDate = DateTime.UtcNow.ToString("yyyyMMdd");
                }

                // Long version string is app version, build date, and git commit
                if (appParams.LongVersion == null) {
                    appParams.LongVersion = appParams.Version + "-" + appParams.BuildDate + "-" + gitVersion.Substring(0, 8);
                }

                if (appParams.Print) {
                    // Print out the version if that what was asked for
                    System.Console.WriteLine(appParams.LongVersion);
                }
                else {
                    WriteVersionFile();
                    UpdateAssemblyFile();
                    WriteAppVersion();
                }
            }
        }

        /// <summary>
        /// Fetch the long form of the current selected GIT version.
        /// </summary>
        /// <returns>The long form of current Git HEAD version or 'null' if cannot be read</returns>
        public string? GetGitVersion() {
            string? gitVersion = null;

            string? gitDir = appParams.GitDir;

            if (gitDir is not null) {
                string headFile = gitDir + "/" + "HEAD";
                if (File.Exists(headFile)) {
                    string refFile = ".git/" + File.ReadAllText(headFile);
                    var refFilePieces = refFile.Split(' ');
                    if (refFilePieces.Length > 1) {
                        if (refFilePieces[0] == ".git/ref:") {
                            refFile = gitDir + "/" + refFilePieces[1].Trim();
                        }
                    }

                    if (File.Exists(refFile)) {
                        gitVersion = File.ReadAllText(refFile).Trim();
                    }
                    else {
                        log.Error("Cannot open GIT ref file named {0}", refFile);
                    }
                }
                else {
                    log.Error("Cannot open GIT HEAD file named {0}", headFile);
                }
            }
            else {
                log.Error("gitDir not specified in app parameters");
            }
            return gitVersion;
        }

        public void OptionallyIncrementBuildNumber(ref string[] pVersionParts) {
            if (appParams.IncrementBuild) {
                try {
                    var buildnum = Convert.ToInt32(pVersionParts[2]);
                    pVersionParts[2] = (buildnum + 1).ToString();
                    appParams.Version = String.Format("{0}.{1}.{2}", pVersionParts);
                    log.Debug("Incremented build number. New version = {0}", appParams.Version);
                }
                catch (Exception ex) {
                    log.Error("Exception incrementing build number: {0}", ex);
                    return;
                }
            }
        }

        public void WriteVersionFile() {
            // Write VersionFile
            if (appParams.VersionFile is not null) {
                log.Debug("Creating version file {0}", appParams.VersionFile);
                try {
                    var buff = new StringBuilder();
                    buff.AppendLine("// This file is auto-generated by BuildVersion");
                    buff.AppendLine("// Before editting, check out the application's build environment for use of BuildVersion");
                    buff.AppendLine("using System;");
                    buff.AppendLine(String.Format("namespace {0} {{", appParams.NameSpace ?? "UNKNOWN"));
                    buff.AppendLine("    public class VersionInfo {");
                    buff.AppendLine(String.Format("        public static string appVersion = \"{0}\";", appParams.Version));
                    buff.AppendLine(String.Format("        public static string longVersion = \"{0}\";", appParams.LongVersion));
                    buff.AppendLine(String.Format("        public static string buildDate = \"{0}\";", appParams.BuildDate));
                    buff.AppendLine("    }");
                    buff.AppendLine("}");
                    File.WriteAllText(appParams.VersionFile ?? "", buff.ToString());
                }
                catch (Exception e) {
                    log.Error("Exception writing version file {0}: {1}", appParams.VersionFile, e);
                }
            }
        }

        public void UpdateAssemblyFile() {
            if (appParams.AssemblyFile is not null) {
                log.Debug("Adding version {0} to {1}", appParams.Version, appParams.AssemblyFile);
                try {
                    string ambly = File.ReadAllText(appParams.AssemblyFile);
                    ambly = Regex.Replace(ambly, @"Version\(""[0-9\.]*""\)", @"Version(""" + appParams.Version + @".0"")");
                    File.WriteAllText(appParams.AssemblyFile, ambly);
                }
                catch (IOException e) {
                    log.Error("Exception writing assembly file {0}: {1}", appParams.AssemblyFile, e);
                }
            }
            return;
        }

        public void WriteAppVersion() {
            if (appParams.WriteAppVersion is not null) {
                File.WriteAllText(appParams.WriteAppVersion, appParams.Version);
            }
        }

        /// <summary>
        /// Process the command line parameters and return an AppParams instance with
        /// the configuration parameters for this session.
        /// </summary>
        /// <param name="args">command line parameters</param>
        /// <returns>initialized AppParams or 'null' if there were errors</returns>
        public static AppParams? GetParams(string[] args) {
            var parms = new AppParams();
            // A single parameter of '--help' outputs the invocation parameters
            if (args.Length > 0 && args[0] == "--help") {
                System.Console.WriteLine(Invocation(parms));
                return null;
            }

            try {
                parms.MergeCommandLine(args);
            }
            catch (Exception e) {
                System.Console.WriteLine("ERROR: bad parameters: " + e.Message);
                System.Console.WriteLine(Invocation(parms));
                return null;
            }

            return parms;
        }

        public static string Invocation(AppParams pParams) {
            StringBuilder buff = new StringBuilder();
            buff.AppendLine("Invocation: BuildVersion <parameters>");
            buff.AppendLine("   Possible parameters are (negate bool parameters by prepending 'no'):");
            string[] paramDescs = pParams.ListParameters().Select(kvp => { return kvp.Key + ":   " + kvp.Value; }).ToArray();
            buff.AppendLine(String.Join(Environment.NewLine, paramDescs));
            return buff.ToString();
        }

    }
}
