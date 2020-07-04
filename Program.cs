using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;

namespace Syncer
{
    public class ImageData
    {
        public string Path { get; set; }
        public string[] Tags { get; set; }
        public string FileName { get; set; }
    }

    public class CandidateTagPoint
    {
        public string Tag { get; set; }
        public int Point { get; set; }
    }

    public class TagsData
    {
        public string Tag { get; set; }
        public int Times { get; set; }
    }

    class Program
    {
        public static List<ImageData> DataMemory = new List<ImageData>();
        public static List<TagsData> TagsAppearance = new List<TagsData>();
        public static string source;
        public static string target;

        public static void Copy(string famousTag, ImageData toCopy, int count)
        {
            var targetTagFolder = Path.Combine(target, famousTag);
            if (famousTag.Length > 15)
            {
                targetTagFolder = Path.Combine(target, "zz_long_tag");
            }
            if (count < 10)
            {
                targetTagFolder = Path.Combine(target, "zz_not_good_tag");
            }
            try
            {
                if (!Directory.Exists(targetTagFolder))
                {
                    Directory.CreateDirectory(targetTagFolder);
                }
            }
            catch (Exception)
            {
                targetTagFolder = Path.Combine(target, "zz_bad_tag");
                Directory.CreateDirectory(targetTagFolder);
            }
            if (File.Exists(toCopy.Path))
            {
                File.Copy(toCopy.Path, Path.Combine(targetTagFolder, toCopy.FileName), true);
            }
            DataMemory.Remove(toCopy);
        }

        public static void GetValues()
        {
            Console.WriteLine("Enter the source folder:");
            source = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(source))
            {
                source = @"C:\Users\andui\Storgram\4K Stogram\anduin2017";
            }
            if (!Directory.Exists(source))
            {
                throw new InvalidOperationException("No this folder!");
            }
            Console.WriteLine("Enter the target folder:");
            target = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(target))
            {
                target = @"C:\Users\andui\OneDrive\Pictures\Digital\Pretty";
            }
            if (!Directory.Exists(target))
            {
                throw new InvalidOperationException("No this folder!");
            }
        }

        static void Main(string[] args)
        {
            GetValues();
            var files = Directory.GetFiles(source).Where(t => t.EndsWith("png") || t.EndsWith("jpg") || t.EndsWith("bmp") || t.EndsWith("jpeg"));
            foreach (var file in files)
            {
                try
                {
                    var image = Image.Load(file, out IImageFormat format);
                    var tagsByte = image.Metadata.ExifProfile.Values[3].Value as byte[];
                    var tagsString = Encoding.UTF8.GetString(tagsByte, 0, tagsByte.Length);
                    var tagsCollection = tagsString
                        .Split('#')
                        .Select(t => t.Replace("\0", "").Trim().ToLower())
                        .Where(t => !string.IsNullOrWhiteSpace(t))
                        .ToArray();
                    var data = new ImageData
                    {
                        Path = file,
                        Tags = tagsCollection,
                        FileName = Path.GetFileName(file)
                    };
                    DataMemory.Add(data);
                    foreach (var tag in tagsCollection)
                    {
                        var key = TagsAppearance.FirstOrDefault(t => t.Tag == tag);
                        if (key == null)
                        {
                            TagsAppearance.Add(new TagsData
                            {
                                Tag = tag,
                                Times = 0
                            });
                        }
                        else
                        {
                            key.Times++;
                        }
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }
            }

            var famousTags = TagsAppearance
                .Where(t => !string.IsNullOrWhiteSpace(t.Tag))
                .OrderByDescending(t => t.Times)
                .Select(t => t.Tag.Trim())
                .ToList();
            foreach (var famousTag in famousTags)
            {
                var filesWithTag = DataMemory.Where(t => t.Tags.Any(p => p.Contains(famousTag))).ToList();
                foreach (var toCopy in filesWithTag)
                {
                    Copy(famousTag, toCopy, filesWithTag.Count);
                }
            }
            foreach (var tocopy in DataMemory.ToList())
            {
                Copy("zz_no_tag", tocopy, int.MaxValue);
            }
            var videos = Directory.GetFiles(source)
                .Where(t => t.EndsWith("mp4"))
                .Select(t => new ImageData
                {
                    Path = t,
                    FileName = Path.GetFileName(t)
                });
            foreach (var video in videos)
            {
                Copy("video", video, int.MaxValue);
            }
            Console.ReadLine();
        }
    }
}
