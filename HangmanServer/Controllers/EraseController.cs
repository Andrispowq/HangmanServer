using System;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace HangmanServer.Controllers
{
    public enum EraseKind
    {
        Data, Account
    }

    public class EraseRequest
    {
        public Guid sessionID { get; set; }
        public string password { get; set; } = "";
        public EraseKind kind { get; set; }
    }

    [ApiController]
    [Route("[controller]")]
    public class EraseController : ControllerBase
    {
        [HttpDelete(Name = "Erase")]
        public IActionResult Erase([FromBody] EraseRequest request)
        {
            UserEraseResult result = new UserEraseResult();
            result.result = false;

            Session? toModify = Connections.FindSessionBySessionID(request.sessionID);
            if(toModify != null)
            {
                User? user = toModify.GetUserData();

                if (user == null)
                {
                    result.message = "Session has no user logged in!";
                }
                else
                {
                    string decrypted = toModify.Decrypt(request.password);
                    string pass_try = RequestHandlers.database.SecurePassword(user.ID, decrypted);
                    string hash = Crypto.GetHashString(pass_try);
                    string hash2 = Crypto.GetHashString(hash);
                    if (user.password_hash2 == hash2)
                    {
                        switch (request.kind)
                        {
                            case EraseKind.Data:
                                user.DeleteUserData();
                                result.result = true;
                                result.message = "Deleted user data";
                                break;
                            case EraseKind.Account:
                                user.DeleteUserData();
                                RequestHandlers.database.DeleteUser(user.username);
                                Connections.sessions.Remove(toModify.GetConnectionID(), out _);
                                result.result = true;
                                result.message = "Deleted user account";
                                break;
                        }
                    }
                    else
                    {
                        result.message = "Password didn't confirm!";
                    }
                }
            }
            else
            {
                result.message = "SessionID not found!";
            }

            return Ok(result);
        }
    }
}

