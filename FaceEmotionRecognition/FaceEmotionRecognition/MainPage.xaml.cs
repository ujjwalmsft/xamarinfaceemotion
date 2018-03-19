using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Http;
using System.Threading.Tasks;
using Xamarin.Forms;
using Plugin.Connectivity;
using Plugin.Media;

using Microsoft.ProjectOxford.Face;
using Microsoft.ProjectOxford.Face.Contract;
using Microsoft.ProjectOxford.Emotion;
using Microsoft.ProjectOxford.Emotion.Contract;
using Microsoft.ProjectOxford.Vision;
using Microsoft.ProjectOxford.Vision.Contract;
using Plugin.Media.Abstractions;
using FaceEmotionRecognition.Model;


namespace FaceEmotionRecognition
{
    public partial class MainPage : ContentPage
    {

        //cognitive API services 
        private readonly IFaceServiceClient faceServiceClient;
        private readonly EmotionServiceClient emotionServiceClient;
        private readonly VisionServiceClient visionClient;

        //data access
        DataService dataService;
        List<CitizenDetails> items;
        Uri icimguri;
        Uri photouri;
        public MainPage()
        {
            InitializeComponent();

            this.faceServiceClient = new FaceServiceClient("  ");  //enter Face API keys
            this.emotionServiceClient = new EmotionServiceClient(" "); //enter Emotion API Keys
            this.visionClient = new VisionServiceClient(" ");  //Enter Vision API Keys

            dataService = new DataService();
        }

        #region
        // Data Access methods
       
        public async void GetData()
        {
            items = await dataService.GetCitizenDetailsAsync();
        }


        #endregion

        private async Task<FaceEmotionDetection> DetectFaceAndEmotionsAsync(MediaFile inputFile)
        {
            if (!CrossConnectivity.Current.IsConnected)
            {
                await DisplayAlert("Network error",
                  "Please check your network connection and retry.", "OK");
                return null;
            }
            try
            {
                // Get emotions from the specified stream
                Emotion[] emotionResult = await
                  emotionServiceClient.RecognizeAsync(inputFile.GetStream());

                // Assuming the picture has one face, retrieve emotions for the
                // first item in the returned array
                var faceEmotion = emotionResult[0]?.Scores.ToRankedList();

                // Create a list of face attributes that the
                // app will need to retrieve
                var requiredFaceAttributes = new FaceAttributeType[] {
                  FaceAttributeType.Age,
                  FaceAttributeType.Gender,
                  FaceAttributeType.Smile,
                  FaceAttributeType.FacialHair,
                  FaceAttributeType.HeadPose,
                  FaceAttributeType.Glasses
                  };

                // Get a list of faces in a picture
                var faces = await faceServiceClient.DetectAsync(inputFile.GetStream(),
                  false, false, requiredFaceAttributes);
                // Assuming there is only one face, store its attributes
                var faceAttributes = faces[0]?.FaceAttributes;

                FaceEmotionDetection faceEmotionDetection = new FaceEmotionDetection();
                faceEmotionDetection.Age = faceAttributes.Age;
                faceEmotionDetection.Emotion = faceEmotion.FirstOrDefault().Key;
                faceEmotionDetection.Glasses = faceAttributes.Glasses.ToString();
                faceEmotionDetection.Smile = faceAttributes.Smile;
                faceEmotionDetection.Gender = faceAttributes.Gender;
                faceEmotionDetection.Moustache = faceAttributes.FacialHair.Moustache;
                faceEmotionDetection.Beard = faceAttributes.FacialHair.Beard;

                // Get the list of computer vision features
                VisualFeature[] visualFeatures = new VisualFeature[] { VisualFeature.Adult,
                VisualFeature.Categories, VisualFeature.Color, VisualFeature.Description,
                VisualFeature.Faces, VisualFeature.ImageType, VisualFeature.Tags };

                AnalysisResult analysisResult = 
                await visionClient.AnalyzeImageAsync(inputFile.GetStream(), visualFeatures);

                faceEmotionDetection.Adult = analysisResult.Adult.IsAdultContent.ToString();

                //check if image is clipart
                int imgClipTypeVal = 0;
                    imgClipTypeVal = analysisResult.ImageType.ClipArtType;
                if (imgClipTypeVal == 0)
                {
                    faceEmotionDetection.ImgTypeClipArt = "Not Clip Art";
                }
                else if (imgClipTypeVal >= 1 && imgClipTypeVal < 3)
                {
                    faceEmotionDetection.ImgTypeClipArt = "Possible Clip Art";
                }
                else
                {
                    faceEmotionDetection.ImgTypeClipArt = "Clip Art";
                }

                //check if image is line drawn
                int imgTypeVal = 0;
                imgTypeVal = analysisResult.ImageType.LineDrawingType;


                if (imgTypeVal == 0) {
                    faceEmotionDetection.ImgTypeLineDrawing = "Not a Line Drawing";
                   }
                else
                {
                    faceEmotionDetection.ImgTypeLineDrawing = "Line Drawing";
                }

                faceEmotionDetection.ImgRacy = analysisResult.Adult.IsRacyContent.ToString();
                faceEmotionDetection.ImgDescription = analysisResult.Description.Captions[0].Text;
                faceEmotionDetection.VisColor = analysisResult.Color.AccentColor.ToString();
                
                //Computer Vision features

               return faceEmotionDetection;
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", ex.Message, "OK");
                return null;
            }
        }



        private async void UploadPictureButton_Clicked(object sender, EventArgs e)
        {
            if (!CrossMedia.Current.IsPickPhotoSupported)
            {
                await DisplayAlert("No upload", "Picking a photo is not supported.", "OK");
                return;
            }
            var file = await CrossMedia.Current.PickPhotoAsync();
            if (file == null)
                return;
            this.Indicator1.IsVisible = true;
            this.Indicator1.IsRunning = true;
                    
            Image1.Source = ImageSource.FromStream(() => file.GetStream());
            
            FaceEmotionDetection theData = await DetectFaceAndEmotionsAsync(file);
            this.BindingContext = theData;

            this.Indicator1.IsRunning = false;
            this.Indicator1.IsVisible = false;                       
        }

        private async void TakePictureButton_Clicked(object sender, EventArgs e)
        {
            await CrossMedia.Current.Initialize();
            if (!CrossMedia.Current.IsCameraAvailable || !CrossMedia.Current.
              IsTakePhotoSupported)
            {
                await DisplayAlert("No Camera", "No camera available.", "OK");
                return;
            }
            var file = await CrossMedia.Current.TakePhotoAsync(new StoreCameraMediaOptions
            {
                SaveToAlbum = true,
                Name = "test.jpg"
            });
            if (file == null)
                return;
            this.Indicator1.IsVisible = true;
            this.Indicator1.IsRunning = true;
            Image1.Source = ImageSource.FromStream(() => file.GetStream());
            FaceEmotionDetection theData = await DetectFaceAndEmotionsAsync(file);
            this.BindingContext = theData;
            this.Indicator1.IsRunning = false;
            this.Indicator1.IsVisible = false;
        }

            private async void EnterNRICButton_Clicked(object sender, EventArgs e)
        {
           
           var text = NRICEntry.Text;

            #region
            //Fix this later
            string imgURL = null;
            string photoURL = null;
            imgURL = dataService.ImgURL(text);
            photoURL = dataService.PhotoURL(text);
            #endregion

           this.TakePictureButton.IsEnabled = true;
           this.TakePictureButton.IsVisible = true;
        this.UploadPictureButton.IsEnabled = true;
        this.UploadPictureButton.IsVisible = true;

            // Image load on Image sources
            
            
            if (imgURL == null)
            {
                await DisplayAlert("IC image not found", "Please re-enter your IC and try again.", "OK");
                return;
            }
            else
            {
               icimguri = new Uri(imgURL);
            }

            if (photoURL == null) {
                await DisplayAlert("Photo with IC not found", "Please re-enter your IC or try registering.", "OK");
                return;
            }
            else
            {
                photouri = new Uri(photoURL);
            }

            this.Indicator1.IsVisible = true;
            this.Indicator1.IsRunning = true;

            HttpClient httpClient = new HttpClient();


            try
            {   
                Image2.Source = ImageSource.FromUri(icimguri);
                Image3.Source = ImageSource.FromUri(photouri);

                Task<Stream> icimgstreamAsync = httpClient.GetStreamAsync(icimguri);
                Task<Stream> photoimgstreamAsync = httpClient.GetStreamAsync(photouri);

                Stream icimgstream = icimgstreamAsync.Result;
                Stream photoimgstream = photoimgstreamAsync.Result;

                Guid icimgfaceid;
                Guid photoimgfaceid;

                using (icimgstream) {
                    var faces = await faceServiceClient.DetectAsync(icimgstream, returnFaceId: true);
                    if (faces.Length > 0)
                        icimgfaceid = faces[0].FaceId;
                    else
                        throw new Exception("No faces found in ic image");
                }

                using (photoimgstream) {
                    var faces = await faceServiceClient.DetectAsync(photoimgstream, returnFaceId: true);
                    if (faces.Length > 0)
                        photoimgfaceid = faces[0].FaceId;
                }

                var result = await faceServiceClient.VerifyAsync(icimgfaceid, photoimgfaceid);

                facematchresult.Text = result.IsIdentical.ToString();
                facematchscore.Text = result.Confidence.ToString();

                
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", ex.Message, "OK");
                return;
            }


            this.Indicator1.IsRunning = false;
            this.Indicator1.IsVisible = false;



            //

        }
    }
}
