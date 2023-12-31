﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using IntelOrca.Biohazard;
using IntelOrca.Biohazard.Model;
using IntelOrca.Biohazard.Room;

namespace emdui
{
    public class Project
    {
        private readonly List<ProjectFile> _projectFiles = new List<ProjectFile>();

        public string MainPath { get; set; }

        public ModelFile MainModel { get; set; }

        public BioVersion Version => MainModel.Version;
        public IReadOnlyList<ProjectFile> Files => _projectFiles;
        public PlwFile[] Weapons => _projectFiles
            .Where(x => x.Kind == ProjectFileKind.Plw)
            .Select(x => (PlwFile)x.Content)
            .ToArray();

        public static Project FromFile(string path)
        {
            return new Project(path);
        }

        private Project(string path)
        {
            MainPath = path;

            if (path.EndsWith(".pld", System.StringComparison.OrdinalIgnoreCase))
            {
                var pldFile = ModelFile.FromFile(path) as PldFile;
                if (pldFile == null)
                    throw new Exception("Not a PLD formatted file.");

                MainModel = pldFile;
                var fileName = Path.GetFileName(path);
                _projectFiles.Add(new ProjectFile(ProjectFileKind.Pld, fileName, pldFile));

                LoadWeapons23(path);
            }
            else if (path.EndsWith(".emd", System.StringComparison.OrdinalIgnoreCase))
            {
                var emdFile = ModelFile.FromFile(path) as EmdFile;
                if (emdFile == null)
                    throw new Exception("Not an EMD formatted file.");

                MainModel = emdFile;
                var fileName = Path.GetFileName(path);
                _projectFiles.Add(new ProjectFile(ProjectFileKind.Emd, fileName, emdFile));

                LoadTexture(path);

                if (fileName.StartsWith("char", StringComparison.OrdinalIgnoreCase))
                {
                    LoadWeapons1(path);
                }
            }
        }

        private void LoadTexture(string emdPath)
        {
            var timPath = Path.ChangeExtension(emdPath, ".tim");
            if (File.Exists(timPath))
            {
                var timFile = new TimFile(timPath);
                _projectFiles.Add(new ProjectFile(ProjectFileKind.Tim, Path.GetFileName(timPath), timFile));
            }
        }

        private void LoadWeapons1(string emdPath)
        {
            var directory = Path.GetDirectoryName(emdPath);
            var files = Directory.GetFiles(directory);
            foreach (var file in files)
            {
                var fileName = Path.GetFileName(file);
                if (fileName.EndsWith(".emw", StringComparison.OrdinalIgnoreCase))
                {
                    LoadWeapon(file);
                }
            }
        }

        private void LoadWeapons23(string pldPath)
        {
            var pldFileName = Path.GetFileName(pldPath);
            if (!Regex.IsMatch(pldFileName, "PL[0-9A-F][0-9A-F].PLD", RegexOptions.IgnoreCase))
                return;

            var pldFileNameWithoutExtension = Path.GetFileNameWithoutExtension(pldFileName);
            var plwRegex = new Regex(pldFileNameWithoutExtension + "W[0-9A-F][0-9A-F].PLW", RegexOptions.IgnoreCase);

            var directory = Path.GetDirectoryName(pldPath);
            var files = Directory.GetFiles(directory);
            foreach (var plwPath in files)
            {
                var plwFileName = Path.GetFileName(plwPath);
                if (plwRegex.IsMatch(plwFileName))
                {
                    LoadWeapon(plwPath);
                }
            }
        }

        public void LoadWeapon(string path)
        {
            var plwFile = ModelFile.FromFile(path) as PlwFile;
            if (plwFile is null)
                return;

            var plwFileName = Path.GetFileName(path);
            _projectFiles.Add(new ProjectFile(ProjectFileKind.Plw, plwFileName, plwFile));
        }

        public void LoadRdt(string path)
        {
            var rdtFile = new Rdt2(BioVersion.Biohazard2, path);
            _projectFiles.Add(new ProjectFile(ProjectFileKind.Rdt, Path.GetFileName(path), rdtFile));
        }

        public TimFile MainTexture
        {
            get
            {
                if (MainModel is EmdFile emdFile && emdFile.Version != BioVersion.Biohazard1)
                {
                    return _projectFiles.FirstOrDefault(x => x.Kind == ProjectFileKind.Tim).Content as TimFile;
                }
                else
                {
                    return MainModel.GetTim(0);
                }
            }
            set
            {
                if (MainModel is EmdFile emdFile && emdFile.Version != BioVersion.Biohazard1)
                {
                    _projectFiles.FirstOrDefault(x => x.Kind == ProjectFileKind.Tim).Content = value;
                }
                else
                {
                    MainModel.SetTim(0, value);
                }
            }
        }

        public void Save() => Save(MainPath);

        public void Save(string path)
        {
            var oldExtension = Path.GetExtension(MainPath);
            var newExtension = Path.GetExtension(path);
            if (!string.Equals(oldExtension, newExtension, StringComparison.OrdinalIgnoreCase))
            {
                throw new Exception("Must save in the same format that was loaded.");
            }

            _projectFiles[0].Filename = Path.GetFileName(path);

            var directory = Path.GetDirectoryName(path);
            foreach (var projectFile in _projectFiles)
            {
                var projectFilePath = Path.Combine(directory, projectFile.Filename);
                projectFile.Save(projectFilePath);
            }

            MainPath = path;
        }
    }

    public class ProjectFile
    {
        public ProjectFileKind Kind { get; set; }
        public string Filename { get; set; }
        public object Content { get; set; }

        public ProjectFile(ProjectFileKind kind, string filename, object content)
        {
            Kind = kind;
            Filename = filename;
            Content = content;
        }

        public void Save(string path)
        {
            if (Content is ModelFile modelFile)
                modelFile.Save(path);
            else if (Content is TimFile timFile)
                timFile.Save(path);
        }

        public override string ToString() => Filename;
    }

    public enum ProjectFileKind
    {
        Emd,
        Pld,
        Tim,
        Plw,
        Rdt,
    }
}
