﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GoogleAuthCruncher;
using RestoreMy2FA.Resources;

namespace RestoreMy2FA
{
    internal class Program
    {
        public const string ArchiveFileType = "*.tar.gz";
        public const string DbFileName = "databases";
        public const string ExportDirectoryName = "Export";

        private static void AddToLog(Exception e) => File.AppendAllLines("Log.txt", new [] {$"[{DateTime.Now}] : {e.Message}. Stack Trace: {e.StackTrace}" } );

        static void Main(string[] args)
        {
            try
            {
                var cruncher = new Cruncher();

                var files = Directory.GetFiles(Directory.GetCurrentDirectory(), ArchiveFileType);

                if (files.Any())
                {
                    SaveCrunched(cruncher.CrunchTitaniumArchive(files.First()));
                }
                else if (File.Exists(DbFileName))
                {
                    SaveCrunched(cruncher.CrunchDbFile(DbFileName));
                }
                else if (!args.Any())
                {
                    ProcessCommandLine(cruncher);
                }
                else if (args.Length != 1)
                {
                    SaveCrunched(cruncher.CrunchTitaniumArchive(args[0]));
                }
                else if (args.Length != 2)
                {
                    Console.WriteLine(Strings.WrongCountOfArguments);
                    Console.WriteLine(Strings.Help);
                }
                else if (!File.Exists(args[1]))
                {
                    Console.WriteLine(Strings.FileDoesNotExist, args[1]);
                }
                else
                {
                    switch (args[0])
                    {
                        case "archive":
                            SaveCrunched(cruncher.CrunchTitaniumArchive(args[1]));
                            break;
                        case "db":
                            SaveCrunched(cruncher.CrunchDbFile(args[1]));
                            break;
                        default:
                            Console.WriteLine(Strings.UnexpectedArgument);
                            break;
                    }
                }
            }
            catch (GoogleAuthDatabaseException e)
            {
                AddToLog(e.InnerException);
                Console.WriteLine(Strings.DbFileUnrecognized);
            }
            catch (GoogleAuthUnarchiverException e)
            {
                AddToLog(e.InnerException);
                Console.WriteLine(Strings.ArchiveFileCannotBeProcessed);
            }
            catch (Exception e)
            {
                AddToLog(e);
                Console.WriteLine(Strings.UnexpectedBehaviour);
            }

            Console.WriteLine(Strings.PressAnyKey);
            Console.ReadKey();
            // TODO may be Wipe QR after approval
            //DirectoryHelper.WipeAndDelete(ExportDirectoryName);
        }

        private static void ProcessCommandLine(Cruncher cruncher)
        {
            Console.WriteLine(Strings.WhatDoYouHave);
            Console.WriteLine(Strings.TitaniumBackupArchive);
            Console.WriteLine(Strings.DatabaseFile);

            while (true)
            {
                var key = Console.ReadKey();
                Console.WriteLine();

                switch (key.KeyChar)
                {
                    case '1':
                        ChosePath(filePath => SaveCrunched(cruncher.CrunchTitaniumArchive(filePath)), "com.google.android.apps.authenticator2.tar.gz");
                        return;
                    case '2':
                        ChosePath(filePath => SaveCrunched(cruncher.CrunchDbFile(filePath)), DbFileName);
                        return;
                }
            }
        }

        private static void ChosePath(Action<string> setPath, string defaultPath)
        {
            Console.WriteLine(Strings.DidYouCopyFileToTheProgramDirectory);

            while (true)
            {
                var key = Console.ReadKey();
                Console.WriteLine();

                switch (key.KeyChar.ToString().ToLower())
                {
                    case "y":

                        if (!File.Exists(defaultPath))
                        {
                            Console.WriteLine(Strings.FileDoesNotExist, defaultPath);
                            return;
                        }

                        setPath(defaultPath);
                        break;
                    case "n":
                        Console.WriteLine(Strings.PleaseInputFilePath);
                        var path = Console.ReadLine();
                        Console.WriteLine();

                        if (!File.Exists(path))
                        {
                            Console.WriteLine(Strings.FileDoesNotExist, path);
                            return;
                        }

                        setPath(path);
                        break;
                }
            }
        }

        private static void SaveCrunched(IEnumerable<BitmapModel> crunched)
        {
            // otpauth://totp/Google%3Amyemail%40gmail.com?secret=7gmdmzctmhpm7i6nrmbom6u5gny7o6la&issuer=Google
            var di = Directory.CreateDirectory(ExportDirectoryName);
            foreach (var bitmapModel in crunched)
            {
                var fileName = Path.GetInvalidFileNameChars().Aggregate(bitmapModel.OriginalName, (current, badChar) => current.Replace(badChar.ToString(), "."));
                bitmapModel.Bitmap.Save(Path.Combine(di.Name, $"{fileName}.bmp"));
            }

            Console.WriteLine(Strings.NKeysExported, crunched.Count());
        }
    }
}
