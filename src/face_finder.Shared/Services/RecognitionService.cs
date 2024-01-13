using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Amazon;
using Amazon.Rekognition;
using Amazon.Rekognition.Model;
using Amazon.Runtime;
using Microsoft.Extensions.Logging;
using face_finder.Shared.Interfaces;
using face_finder.Shared.Models;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Metadata.Profiles.Exif;
using Image = Amazon.Rekognition.Model.Image;

namespace face_finder.Shared.Services
{
    public class RecognitionService : IRecognitionService
    {
        private const float FaceMatchThreshold = 85F;

        private readonly ILogger<RecognitionService> _logger;
        private readonly AmazonRekognitionClient _client;

        private readonly List<string> _collectionIds = new List<string>
            {"BlackBookBelarusCollection", "PassportCollection","celeb-db"};

        public RecognitionService(ILogger<RecognitionService> logger)
        {
            _logger = logger;
            var config = new AmazonRekognitionConfig
            {
                MaxErrorRetry = 5,
                RegionEndpoint = RegionEndpoint.USEast1,
            };
            _client = new AmazonRekognitionClient(config);
        }

        public RecognitionService(ILogger<RecognitionService> logger, AWSCredentials credentials)
        {
            _logger = logger;
            var config = new AmazonRekognitionConfig
            {
                MaxErrorRetry = 5,
                RegionEndpoint = RegionEndpoint.USEast1,
            };
            _client = new AmazonRekognitionClient(credentials, config);
        }

        public async Task<RecognitionResult> ProcessImageAsync(string base64Image, CancellationToken cancellationToken = default)
        {
            try
            {
                var originalImageInBytes = Convert.FromBase64String(base64Image);

                using var rawImage = SixLabors.ImageSharp.Image.Load(originalImageInBytes, out var imageFormat);
                var rotate = GetExifOrientation(rawImage);
                var isRotated = rotate == OrientationMode.RightTop;
                //image.Mutate(x => x.AutoOrient());
                IImageEncoder imageEncoderForJpeg = new JpegEncoder()
                {
                    Quality = 80
                };

                using var ms = new MemoryStream();
                rawImage.Save(ms, imageEncoderForJpeg);
                originalImageInBytes = ms.ToArray();

                var originalImageStream = new MemoryStream(originalImageInBytes);

                var detectedFaces = await DetectFacesAsync(originalImageStream, cancellationToken);

                var result = new RecognitionResult
                {
                    Subjects = new List<Subject>()
                };

                if (detectedFaces != null && detectedFaces.FaceDetails?.Count > 0)
                {
                    var originalImageInImage = SixLabors.ImageSharp.Image.Load(originalImageStream, out var format);
                    var imageHeight = originalImageInImage.Height;
                    var imageWidth = originalImageInImage.Width;
                    var id = 1;

                    foreach (var faceDetails in detectedFaces.FaceDetails.OrderBy(x => x.BoundingBox.Left).Take(20))
                    {
                        var xF = faceDetails.BoundingBox.Left * imageWidth;
                        var yF = faceDetails.BoundingBox.Top * imageHeight;
                        var widthF = faceDetails.BoundingBox.Width * imageWidth;
                        var heightF = faceDetails.BoundingBox.Height * imageHeight;

                        var x = Convert.ToInt32(Math.Truncate(xF - widthF * 0.1));
                        var y = Convert.ToInt32(Math.Ceiling(yF - heightF * 0.1));
                        var width = Convert.ToInt32(Math.Ceiling(widthF + widthF * 0.2));
                        var height = Convert.ToInt32(Math.Ceiling(heightF + heightF * 0.2));

                        _logger.LogTrace($"image: w:{imageWidth} h:{imageHeight} rot:{isRotated} face: x:{x} y:{y} w:{width} h:{height}");

                        if (width < 20 || height < 20)
                            continue;

                        try
                        {
                            var faceImage = originalImageInImage.CropImage(x, y, width, height);

                            var person = await SearchFacesAsync(faceImage, _collectionIds[2], cancellationToken);

                            result.Subjects.Add(new Subject
                            {
                                Id = id,
                                Box = new FacesBox
                                {
                                    XMax = isRotated ? imageHeight - y : x + width,
                                    YMax = isRotated ? x + width : y,
                                    XMin = isRotated ? imageHeight - y - height : x,
                                    YMin = isRotated ? x : y + height
                                    //XMax = x + width,
                                    //YMax = y,
                                    //XMin = x,
                                    //YMin = y + height

                                    //var box = new FacesBoxDto
                                    //{
                                    //    XMax = isRotated ? Convert.ToInt32(Math.Ceiling(subject.Box.YMin)) : Convert.ToInt32(Math.Truncate(subject.Box.XMax)),
                                    //    XMin = isRotated ? Convert.ToInt32(Math.Truncate(subject.Box.YMax)) : Convert.ToInt32(Math.Ceiling(subject.Box.XMin)),
                                    //    YMax = isRotated ? Convert.ToInt32(Math.Ceiling(subject.Box.XMax)) : Convert.ToInt32(Math.Ceiling(subject.Box.YMax)),
                                    //    YMin = isRotated ? Convert.ToInt32(Math.Ceiling(subject.Box.XMin)) : Convert.ToInt32(Math.Ceiling(subject.Box.YMin))
                                    //};
                                },
                                Person = new Person
                                {
                                    FullNameHash = person?.FaceMatches.Count > 0 ? person.FaceMatches[0]?.Face?.ExternalImageId : string.Empty,
                                    Similarity = person?.FaceMatches.Count > 0
                                        ? Convert.ToInt32(person.FaceMatches[0].Similarity * 100) / 100.0
                                        : 0
                                }
                            }); ;

                            id++;
                        }
                        catch (Exception e)
                        {
                            _logger.LogError(e.ToString());
                        }
                    }
                }

                return result;
            }
            catch (Exception e)
            {
                _logger.LogError(e.ToString());
                return null;
            }
        }

        public async Task<bool> CreateCollectionAsync(string collectionId,
            CancellationToken cancellationToken = default)
        {
            var createCollectionRequest = new CreateCollectionRequest
            {
                CollectionId = collectionId
            };

            try
            {
                await _client.CreateCollectionAsync(createCollectionRequest, cancellationToken);
                return true;
            }
            catch (Exception e)
            {
                _logger.LogError(e.ToString());
                return false;
            }
        }

        public async Task<bool> DeleteCollectionAsync(string collectionId,
            CancellationToken cancellationToken = default)
        {
            var deleteCollectionRequest = new DeleteCollectionRequest
            {
                CollectionId = collectionId
            };

            try
            {
                await _client.DeleteCollectionAsync(deleteCollectionRequest, cancellationToken);
                return true;
            }
            catch (Exception e)
            {
                _logger.LogError(e.ToString());
                return false;
            }
        }

        public async Task<List<string>> ListCollectionsAsync(CancellationToken cancellationToken)
        {
            const int limit = 10;

            ListCollectionsResponse listCollectionsResponse = null;
            var result = new List<string>();
            string paginationToken = null;
            try
            {
                do
                {
                    if (listCollectionsResponse != null)
                        paginationToken = listCollectionsResponse.NextToken;

                    var listCollectionsRequest = new ListCollectionsRequest
                    {
                        MaxResults = limit,
                        NextToken = paginationToken
                    };

                    listCollectionsResponse =
                        await _client.ListCollectionsAsync(listCollectionsRequest, cancellationToken);

                    result.AddRange(listCollectionsResponse.CollectionIds);
                } while (listCollectionsResponse.NextToken != null);
            }
            catch (Exception e)
            {
                _logger.LogError(e.ToString());
            }

            return result;
        }

        public async Task<List<Face>> ListFacesAsync(string collectionId, CancellationToken cancellationToken = default)
        {
            ListFacesResponse listFacesResponse = null;
            var result = new List<Face>();
            string paginationToken = null;

            try
            {
                do
                {
                    if (listFacesResponse != null)
                        paginationToken = listFacesResponse.NextToken;

                    var listFacesRequest = new ListFacesRequest
                    {
                        CollectionId = collectionId,
                        MaxResults = 1,
                        NextToken = paginationToken
                    };

                    listFacesResponse = await _client.ListFacesAsync(listFacesRequest, cancellationToken);
                    foreach (var face in listFacesResponse.Faces)
                    {
                        result.Add(face);
                    }
                } while (!string.IsNullOrEmpty(listFacesResponse.NextToken));

                return result;
            }
            catch (Exception e)
            {
                _logger.LogError(e.ToString());
                return null;
            }
        }

        public async Task<bool> AddFaceToCollectionAsync(MemoryStream face, string name, string collectionId,
            CancellationToken cancellationToken = default)
        {
            if (face == null) throw new ArgumentNullException(nameof(face));
            if (name == null) throw new ArgumentNullException(nameof(name));

            var indexFacesRequest = new IndexFacesRequest
            {
                Image = new Image
                {
                    Bytes = face
                },
                CollectionId = collectionId,
                ExternalImageId = name,
                DetectionAttributes = new List<string> { "ALL" }
            };

            try
            {
                var indexFacesResponse = await _client.IndexFacesAsync(indexFacesRequest, cancellationToken);
                return true;
            }
            catch (Exception e)
            {
                _logger.LogError(e.ToString());
                return false;
            }
        }

        public async Task<bool> AddFaceToCollectionAsync(string bucketName, string name, string collectionId,
            CancellationToken cancellationToken = default)
        {
            if (bucketName == null) throw new ArgumentNullException(nameof(bucketName));
            if (name == null) throw new ArgumentNullException(nameof(name));

            var image = new Image
            {
                S3Object = new S3Object
                {
                    Bucket = bucketName,
                    Name = name
                }
            };

            var indexFacesRequest = new IndexFacesRequest
            {
                Image = image,
                CollectionId = collectionId,
                ExternalImageId = Path.GetDirectoryName(name).ConvertAsciiStringToHexString(),
                DetectionAttributes = new List<string> { "ALL" }
            };

            try
            {
                var indexFacesResponse = await _client.IndexFacesAsync(indexFacesRequest, cancellationToken);
                return true;
            }
            catch (Exception e)
            {
                _logger.LogError(e.ToString());
                return false;
            }
        }

        public async Task<SearchFacesByImageResponse> SearchFacesAsync(MemoryStream image, string collectionId,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var searchFacesByImageRequest = new SearchFacesByImageRequest
                {
                    CollectionId = collectionId,
                    Image = new Image
                    {
                        Bytes = image
                    },
                    FaceMatchThreshold = FaceMatchThreshold
                };

                var searchFacesByImageResponse =
                    await _client.SearchFacesByImageAsync(searchFacesByImageRequest, cancellationToken);

                return searchFacesByImageResponse;
            }
            catch (Exception e)
            {
                _logger.LogError(e.ToString());
                return null;
            }
        }

        public async Task<DetectFacesResponse> DetectFacesAsync(MemoryStream image,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var detectFacesRequest = new DetectFacesRequest
                {
                    Image = new Image
                    {
                        Bytes = image
                    },
                    // Attributes can be "ALL" or "DEFAULT".
                    // "DEFAULT": BoundingBox, Confidence, Landmarks, Pose, and Quality.
                    // "ALL": See https://docs.aws.amazon.com/sdkfornet/v3/apidocs/items/Rekognition/TFaceDetail.html
                    Attributes = new List<string>() { "DEFAULT" }
                };

                var detectFacesResponse = await _client.DetectFacesAsync(detectFacesRequest, cancellationToken);
                return detectFacesResponse;
            }
            catch (Exception e)
            {
                _logger.LogError(e.ToString());
                return null;
            }
        }

        private OrientationMode GetExifOrientation(SixLabors.ImageSharp.Image source)
        {
            if (source.Metadata.ExifProfile is null)
            {
                return OrientationMode.Unknown;
            }

            IExifValue<ushort> value = source.Metadata.ExifProfile.GetValue(ExifTag.Orientation);
            if (value is null)
            {
                return OrientationMode.Unknown;
            }

            OrientationMode orientation;
            if (value.DataType == ExifDataType.Short)
            {
                orientation = (OrientationMode)value.Value;
            }
            else
            {
                orientation = (OrientationMode)Convert.ToUInt16(value.Value);
                source.Metadata.ExifProfile.RemoveValue(ExifTag.Orientation);
            }

            source.Metadata.ExifProfile.SetValue(ExifTag.Orientation, (ushort)OrientationMode.TopLeft);

            return orientation;
        }

        /// <summary>
        /// Enumerates the available orientation values supplied by EXIF metadata.
        /// </summary>
        internal enum OrientationMode : ushort
        {
            /// <summary>
            /// Unknown rotation.
            /// </summary>
            Unknown = 0,

            /// <summary>
            /// The 0th row at the top, the 0th column on the left.
            /// </summary>
            TopLeft = 1,

            /// <summary>
            /// The 0th row at the top, the 0th column on the right.
            /// </summary>
            TopRight = 2,

            /// <summary>
            /// The 0th row at the bottom, the 0th column on the right.
            /// </summary>
            BottomRight = 3,

            /// <summary>
            /// The 0th row at the bottom, the 0th column on the left.
            /// </summary>
            BottomLeft = 4,

            /// <summary>
            /// The 0th row on the left, the 0th column at the top.
            /// </summary>
            LeftTop = 5,

            /// <summary>
            /// The 0th row at the right, the 0th column at the top.
            /// </summary>
            RightTop = 6,

            /// <summary>
            /// The 0th row on the right, the 0th column at the bottom.
            /// </summary>
            RightBottom = 7,

            /// <summary>
            /// The 0th row on the left, the 0th column at the bottom.
            /// </summary>
            LeftBottom = 8
        }
    }
}