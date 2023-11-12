using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Text.Unicode;
using CsvHelper;
using Microsoft.VisualBasic.FileIO;
using face_finder.Shared;
using face_finder.Shared.Models;
using Xunit;
using Xunit.Abstractions;

namespace face_finder.IntegrationTests
{
    public class DataPreparationTests
    {
        private readonly ITestOutputHelper _output;

        public DataPreparationTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void PrepareFolderStructureTest()
        {
            const string sourcePath = @"E:\\source";
            const string resultPath = "E:\\result";
            if (Directory.Exists(resultPath))
            {
                Directory.Delete(resultPath, true);
            }

            Directory.CreateDirectory("E:\\result");

            var dirs = new List<string>(Directory.EnumerateDirectories(sourcePath));

            foreach (var dir in dirs)
            {
                var folderName = dir.Substring(dir.LastIndexOf('\\') + 1);
                var imagePath = Path.Combine(sourcePath, folderName, "samples", "pivot.jpg");
                var descriptionPath = Path.Combine(sourcePath, folderName, "indictment.txt");

                if (!File.Exists(imagePath) || !File.Exists(descriptionPath))
                    continue;

                Directory.CreateDirectory($"{resultPath}\\{folderName}");
                File.Copy(imagePath, Path.Combine(resultPath, folderName, "pivot.jpg"), true);
                File.Copy(descriptionPath, Path.Combine(resultPath, folderName, "indictment.txt"), true);
            }

            var resultDirs = new List<string>(Directory.EnumerateDirectories(resultPath));

            _output.WriteLine("{0} directories found in source.", dirs.Count);
            _output.WriteLine("{0} directories was created.", resultDirs.Count);
        }

        [Fact]
        public void PrepareFolderStructurePassportBaseTest()
        {
            const string sourcePath = @"E:\\passport_base";
            const string resultPath = "E:\\passport_base_result";
            if (Directory.Exists(resultPath))
            {
                Directory.Delete(resultPath, true);
            }

            Directory.CreateDirectory(resultPath);

            var files = new List<string>(Directory.EnumerateFiles(sourcePath));

            foreach (var file in files)
            {
                var identityNumber = Path.GetFileNameWithoutExtension(file);
                var imagePath = Path.Combine(sourcePath, file);

                Directory.CreateDirectory($"{resultPath}\\{identityNumber}");
                File.Copy(imagePath, Path.Combine(resultPath, identityNumber, "original.jpeg"), true);
                var personalData = new PersonalData
                {
                    FirstName = string.Empty,
                    LastName = string.Empty,
                    Patronym = string.Empty,
                    IdentityNumber = identityNumber,
                    Dob = DateTime.MinValue,
                    Rank = string.Empty,
                    Position = string.Empty,
                    Addresses = new[]
                    {
                        new Address
                        {
                            Country = "Belarus",
                            City = string.Empty,
                            Street = string.Empty,
                            Building = string.Empty,
                            Apartment = string.Empty,
                            Note = string.Empty
                        }
                    },
                    PhoneNumbers = new[]
                    {
                        new PhoneNumber
                        {
                            Number = string.Empty,
                            Note = string.Empty
                        },
                        new PhoneNumber
                        {
                            Number = string.Empty,
                            Note = string.Empty
                        }
                    },
                    Other = string.Empty
                };

                var json = JsonSerializer.Serialize(personalData, Extensions.GetJsonSerializerOptions());
                File.WriteAllText(Path.Combine(resultPath, identityNumber, "personal_data.json"), json);
            }

            var resultDirs = new List<string>(Directory.EnumerateDirectories(resultPath));

            _output.WriteLine("{0} directories found in source.", files.Count);
            _output.WriteLine("{0} directories was created.", resultDirs.Count);
        }

        [Fact]
        public void SearchForDuplicatesTest()
        {
            const string passportBasePath = @"E:\\passport_base";
            const string czgbPath = "E:\\result";

            var ids = new List<string>(Directory.EnumerateFiles(passportBasePath));
            var folders = new List<string>(Directory.EnumerateDirectories(czgbPath));

            var result = new List<string>();

            foreach (var folder in folders)
            {
                var file = File.ReadLines(Path.Combine(folder, "indictment.txt"));

                foreach (var id in ids)
                {
                    var identityNumber = Path.GetFileNameWithoutExtension(id);
                    if (file.Any(line => line.Contains(identityNumber)))
                    {
                        result.Add($"Folder name: {Path.GetFileName(folder)}. Identity number: {identityNumber}");
                    }
                }
            }

            File.WriteAllLines("E:\\duplicates.txt", result);
        }

        [Fact]
        public void SerializeJsonToFileTest()
        {
            var personalData = new PersonalData
            {
                FirstName = "Александр",
                LastName = "Алёкса",
                Patronym = "Иванович",
                IdentityNumber = "3070478A082PB7",
                Dob = new DateTime(1978, 04, 07),
                Rank = "полковник милиции",
                Position = "заместитель начальника 3 управления ГУБОПиК - начальник 1 отдела",
                Addresses = new[]
                {
                    new Address
                    {
                        Country = "Belarus",
                        City = "Минск",
                        Street = "Владислава Голубка",
                        Building = "12",
                        Apartment = "27",
                        Note = string.Empty
                    }
                },
                PhoneNumbers = new[]
                {
                    new PhoneNumber
                    {
                        Number = "+375296424501",
                        Note = "личный"
                    },
                    new PhoneNumber
                    {
                        Number = "+375336067355",
                        Note = "дополнительный"
                    }
                },
                Other = string.Empty
            };

            var json = JsonSerializer.Serialize(personalData, Extensions.GetJsonSerializerOptions());
            File.WriteAllText("E://personal_data.json", json);
        }

        [Fact]
        public void DeserializeJsonFromFileTest()
        {
            var jsonString = File.ReadAllText("E://personal_data.json");
            var personalData = JsonSerializer.Deserialize<PersonalData>(jsonString, Extensions.GetJsonSerializerOptions());
        }

        [Fact]
        public void PrepareFolderStructurePassportBaseTest2()
        {
            const string sourcePath = "E:\\passport_base";
            const string resultPath = "E:\\passport_base_new_result";
            if (Directory.Exists(resultPath))
            {
                Directory.Delete(resultPath, true);
            }

            Directory.CreateDirectory(resultPath);

            var folders = new List<string>(Directory.EnumerateDirectories(sourcePath));
            var peopleWithoutInfo = new List<string>();

            var personsFromBase = GetCsvContent().ToList();

            foreach (var person in personsFromBase)
            {
                var path = folders.FirstOrDefault(x => x.Contains(person.IdentityNumber));
                if (string.IsNullOrEmpty(path))
                    continue;

                var identityNumber = person.IdentityNumber;

                var jsonString = File.ReadAllText(Path.Combine(sourcePath, identityNumber, "personal_data.json"));
                var personalDataOld = JsonSerializer.Deserialize<PersonalData>(jsonString, Extensions.GetJsonSerializerOptions());

                if (!string.IsNullOrEmpty(personalDataOld.Other))
                {
                    _output.WriteLine(identityNumber);
                    continue;
                }

                //var imagePath = Path.Combine(sourcePath, identityNumber);
                Directory.CreateDirectory($"{resultPath}\\{identityNumber}");
                //File.Copy(imagePath, Path.Combine(resultPath, identityNumber, "original.jpeg"), true);

                var personalData = new PersonalData
                {
                    FullName = person.FullName,
                    FirstName = person.FirstName,
                    LastName = person.LastName,
                    Patronym = person.Patronym,
                    IdentityNumber = identityNumber,
                    Dob = person.Dob.Value,
                    Rank = string.Empty,
                    Position = string.Empty,
                    Addresses = new[]
                    {
                        new Address
                        {
                            Country = "Belarus",
                            City = string.Empty,
                            Street = string.Empty,
                            Building = string.Empty,
                            Apartment = string.Empty,
                            Note = string.Empty
                        }
                    },
                    PhoneNumbers = new[]
                    {
                        new PhoneNumber
                        {
                            Number = string.Empty,
                            Note = string.Empty
                        },
                        new PhoneNumber
                        {
                            Number = string.Empty,
                            Note = string.Empty
                        }
                    },
                    Emails = new[]
                    {
                        new Email
                        {
                            EmailAddress = string.Empty,
                            Note = string.Empty
                        }
                    },
                    SocialMedia = new[]
                    {
                        new SocialMedia
                        {
                            Link = string.Empty,
                            Note = string.Empty
                        }
                    },
                    Other = string.Empty,
                    Source = string.Empty
                };

                var json = JsonSerializer.Serialize(personalData, Extensions.GetJsonSerializerOptions());
                File.WriteAllText(Path.Combine(resultPath, identityNumber, "personal_data.json"), json);
            }
        }

        [Fact]
        public void PrepareFolderStructurePassportBaseTest3()
        {
            const string sourcePath = "E:\\passport_base";

            var folders = new List<string>(Directory.EnumerateDirectories(sourcePath));

            foreach (var folder in folders)
            {
                var jsonString = File.ReadAllText(Path.Combine(folder, "personal_data.json"));

                PersonalData personalData;

                try
                {
                    personalData = JsonSerializer.Deserialize<PersonalData>(jsonString, Extensions.GetJsonSerializerOptions());
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    throw;
                }

                if (string.IsNullOrEmpty(personalData.Other) || !string.IsNullOrEmpty(personalData.FullName))
                    continue;

                var fullName = personalData.Other.Split("\n")[0].Trim();
                _output.WriteLine(folder);
                var names = fullName.Split(" ");

                //if (names.Length == 3)
                //{
                //    personalData.FullName = fullName;
                //    personalData.LastName = names[0];
                //    personalData.FirstName = names[1];
                //    personalData.Patronym = names[2];
                //    personalData.Emails = new[]
                //    {
                //        new Email
                //        {
                //            EmailAddress = string.Empty,
                //            Note = string.Empty
                //        }
                //    };
                //    personalData.SocialMedia = new[]
                //    {
                //        new SocialMedia
                //        {
                //            Link = string.Empty,
                //            Note = string.Empty
                //        }
                //    };
                //    personalData.Other = string.Empty;

                //    if (string.IsNullOrEmpty(personalData.Source))
                //        personalData.Source = string.Empty;

                //    var json = JsonSerializer.Serialize(personalData, options);
                //    File.WriteAllText(Path.Combine(folder, "personal_data.json"), json);
                //    _output.WriteLine(fullName);
                //}
            }
        }

        private IEnumerable<PersonFromBase> GetCsvContent()
        {
            using var reader = new StreamReader("E:\\face_finder\\111.csv");
            using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
            var records = new List<PersonFromBase>();
            while (csv.Read())
            {
                var fullName = csv.GetField(1).ToLower();
                fullName = Regex.Replace(fullName, @"(^\w)|(\s\w)", m => m.Value.ToUpper());
                var names = fullName.Split(' ');

                records.Add(new PersonFromBase
                {
                    IdentityNumber = csv.GetField(0),
                    FullName = fullName,
                    LastName = names[0],
                    FirstName = names[1],
                    Patronym = names[2],
                    Dob = DateTime.ParseExact(csv.GetField(2), "yyyy-MM-dd", CultureInfo.InvariantCulture)
                });
            }

            return records;
        }
    }

    public class PersonFromBase
    {
        public string IdentityNumber { get; set; }

        public string FullName { get; set; }

        public DateTime? Dob { get; set; }

        public string FirstName { get; set; }

        public string LastName { get; set; }

        public string Patronym { get; set; }
    }
}