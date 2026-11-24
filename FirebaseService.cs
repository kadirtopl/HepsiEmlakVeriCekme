using Firebase.Database;
using Firebase.Database.Query;
using System.Text.Json;

namespace SahibindenScraper
{
    public class FirebaseService
    {
        private readonly FirebaseClient _firebaseClient;
        private static readonly string DATABASE_URL = Environment.GetEnvironmentVariable("FIREBASE_DATABASE_URL") ?? "https://PROJE-ID-default-rtdb.europe-west1.firebasedatabase.app/";
        private Random _random = new Random();

        public FirebaseService()
        {
            _firebaseClient = new FirebaseClient(DATABASE_URL);
        }

        public async Task<bool> CheckIfListingExists(string listingId)
        {
            try
            {
                if (string.IsNullOrEmpty(listingId))
                {
                    return false;
                }

                var listing = await _firebaseClient
                    .Child("emlakhepsiis")
                    .Child("kocaeli")
                    .Child("izmit")
                    .Child(listingId)
                    .OnceSingleAsync<object>();

                return listing != null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Firebase kontrol sırasında hata oluştu: {ex.Message}");
                // Hata durumunda true dönüyoruz ki ilan atlansın
                return true;
            }
        }

        public async Task<bool> SaveListingData(Dictionary<string, string> listingData)
        {
            try
            {
                if (listingData == null || !listingData.ContainsKey("İlan ID"))
                {
                    Console.WriteLine("İlan ID bulunamadı");
                    return false;
                }

                var listingId = listingData["İlan ID"];
                
                // Önce ilanın Firebase'de olup olmadığını kontrol et
                var existingListing = await _firebaseClient
                    .Child("emlakhepsiis")
                    .Child("kocaeli")
                    .Child("izmit")
                    .Child(listingId)
                    .OnceSingleAsync<object>();

                // Eğer ilan zaten varsa, kaydetme
                if (existingListing != null)
                {
                    Console.WriteLine($"İlan zaten mevcut: {listingId}");
                    return false;
                }

                var city = listingData.GetValueOrDefault("Şehir", "kocaeli").ToLower();
                var district = listingData.GetValueOrDefault("İlçe", "izmit").ToLower();

                var listing = new
                {
                    ilanId = listingId,
                    baslik = listingData.GetValueOrDefault("Başlık", ""),
                    fiyat = listingData.GetValueOrDefault("Fiyat", ""),
                    metrekare = listingData.GetValueOrDefault("m² (Brüt)", ""),
                    odaSayisi = listingData.GetValueOrDefault("Oda Sayısı", ""),
                    sehir = city,
                    ilce = district,
                    ilanTarihi = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    yeniIlan = "Evet",
                    ilanSahibi = listingData.GetValueOrDefault("İlan Sahibi", ""),
                    telefon = listingData.GetValueOrDefault("Telefon", ""),
                    binaYasi = listingData.GetValueOrDefault("Bina Yaşı", ""),
                    bulunduguKat = listingData.GetValueOrDefault("Bulunduğu Kat", "")
                };

                await _firebaseClient
                    .Child("emlakhepsiis")
                    .Child(city)
                    .Child(district)
                    .Child(listingId)
                    .PutAsync(listing);

                Console.WriteLine($"İlan başarıyla kaydedildi: {listingId}");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Firebase'e kayıt sırasında hata oluştu: {ex.Message}");
                return false;
            }
        }
    }
} 