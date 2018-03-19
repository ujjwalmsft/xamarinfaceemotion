using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;
using Newtonsoft.Json;

using FaceEmotionRecognition.Model;



namespace FaceEmotionRecognition
{
        
   public class DataService
    {
        HttpClient client = new HttpClient();
        public DataService()
        {
        }

        /// <summary>
        /// Gets the CitizenDetails async.
        /// </summary>
        /// <returns>The CitizenDetails async.</returns>
        public async Task<List<CitizenDetails>> GetCitizenDetailsAsync()
        {

                   var response = await client.GetStringAsync("https://citizen.json");   //enter the JSON path with user details
                   var citizenDetails = JsonConvert.DeserializeObject<List<CitizenDetails>>(response);
                   return citizenDetails;  
        }

        //Todo
        //Implement write delete methods

        public string ImgURL(string getval)
        {
            string imgURL = null;

            imgURL = "https://NRIC_Han.jpg";    //update with NRIC image url
          
            return imgURL;
        }

        public string PhotoURL(string getval)
        {
            string imgURL = null;
            
            imgURL = "https://Photo_Han.jpg";    //update with Photo URL
         
            return imgURL;
        }
    }

}
