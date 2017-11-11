using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace BumpVersion
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var solutions = Directory.GetDirectories(Directory.GetCurrentDirectory()).Select(dir => new Solution(dir, new Project(Path.Combine(dir, Path.GetFileName(dir), $"{Path.GetFileName(dir)}.csproj"))));

            if (!solutions.Any(s => s.Project.Exists()))
            {
                var path = Directory.GetCurrentDirectory();
                var projectName = $"{Path.GetFileName(path)}.csproj";
                var projectPath = Path.Combine(path, Path.GetFileName(path), projectName);

                var solution = new Solution(Directory.GetCurrentDirectory(), new Project(projectPath));

                if (solution.HasUncommittedChanges())
                {
                    Console.WriteLine($"{solution.Name}: Uncommitted changes");
                    Console.WriteLine($"Aborting.");
                    Console.ReadLine();
                    return;
                }

                solution.Project.Bump(3);
                solution.Project.Save();

                solution.Commit($"Bumped version");

                return;
            }
        }

        public class Solution
        {
            public string Name { get; }
            public string Path { get; }
            public Project Project { get; }

            public Solution(string path, Project project)
            {
                Name = System.IO.Path.GetFileName(path);
                Path = path;
                //Repository = new LibGit2Sharp.Repository(path);
                Project = project;
            }

            public void Commit(string message)
            {
                RunCommand("add .");
                RunCommand($"commit -m \"{message}\"");
            }

            public bool HasUncommittedChanges()
            {
                return !string.IsNullOrWhiteSpace(RunCommand("status --porcelain"));
            }

            string RunCommand(string args)
            {
                var processInfo = new ProcessStartInfo
                {
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    FileName = "git.exe",
                    CreateNoWindow = true,
                    WorkingDirectory = Path,
                };

                var gitProcess = new Process();
                gitProcess.StartInfo = processInfo;
                gitProcess.StartInfo.Arguments = args;
                gitProcess.Start();
                var output = gitProcess.StandardOutput.ReadToEnd().Trim();
                gitProcess.WaitForExit();
                return output;
            }
        }

        public class Project
        {
            public string Name { get; }
            string Path { get; }
            XElement Element { get; }

            public Project(string path)
            {
                Name = System.IO.Path.GetFileNameWithoutExtension(path);
                Path = path;

                if (Exists())
                {
                    Element = XElement.Load(path);
                }
            }

            public Version AssemblyVersion
            {
                get
                {
                    return new Version(Element.Descendants("AssemblyVersion").First().Value);
                }
                set
                {
                    Element.Descendants("AssemblyVersion").First().Value = value.ToString();
                }
            }

            public Version Version
            {
                get
                {
                    return new Version(Element.Descendants("Version").First().Value);
                }
                set
                {
                    Element.Descendants("Version").First().Value = value.ToString();
                }
            }

            public IEnumerable<Dependency> Dependencies
            {
                get
                {
                    return Element.Descendants("PackageReference").Select(element => new Dependency(element));
                }
            }

            public bool Exists()
            {
                return File.Exists(Path);
            }

            public IEnumerable<Dependency> GetOldDependencies(IEnumerable<Project> projects)
            {
                foreach (var dependency in Dependencies)
                {
                    var project = dependency.GetProject(projects);

                    if (project == null)
                    {
                        continue;
                    }

                    if (dependency.Version < project.Version)
                    {
                        yield return dependency;
                    }
                }
            }

            public void Bump(int segmentNumber)
            {
                Version = Version.Bump(segmentNumber);
                AssemblyVersion = AssemblyVersion.Bump(segmentNumber);
            }

            public void Save()
            {
                File.WriteAllText(Path, Element.ToString() + Environment.NewLine);
            }

            public override string ToString()
            {
                return Element.ToString();
            }
        }

        public class Dependency
        {
            XElement Element { get; }

            public Dependency(XElement element)
            {
                Element = element;
            }

            public string Name
            {
                get
                {
                    return Element.Attribute("Include").Value;
                }
                set
                {
                    Element.Attribute("Include").Value = value;
                }
            }

            public Version Version
            {
                get
                {
                    return new Version(Element.Attribute("Version").Value + ".0");
                }
                set
                {
                    var version = value.ToString();
                    var segments = version.Split('.');

                    if (segments.Length == 4)
                    {
                        version = version.Remove(version.Length - 1 - segments.Last().Length);
                    }

                    Element.Attribute("Version").Value = version;
                }
            }

            public Project GetProject(IEnumerable<Project> projects)
            {
                return projects.SingleOrDefault(project => project.Name == Name);
            }
        }
    }
}