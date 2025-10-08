namespace EbayChatBot.API.DTOs
{
    public class loginSignUpDto
    {
        public class RegisterDto
        {
            public string Email { get; set; }
            public string Password { get; set; }
            public string FirstName { get; set; }
            public string LastName { get; set; }
            public string EbayUserName { get; set; }
        }

        public class LoginDto
        {
            public string Email { get; set; }
            public string Password { get; set; }
        }
    }
}
