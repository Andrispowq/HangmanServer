using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Formatters;

namespace HangmanServer.src.Controllers.Dictionary
{
    [ApiController]
    [Route("Dictionary/[controller]")]
    public class DownloadController : Controller
    {
        [HttpGet(Name = "Download")]
        public IActionResult Download([FromQuery] string language = "hu")
        {
            if (language == "hu")
            {
                var filePath = $"{Environment.CurrentDirectory}/HangmanServerData/magyar_szavak.txt";
                var contentType = "application/octet-stream";
                var fileName = Path.GetFileName(filePath);
                return PhysicalFile(filePath, contentType, fileName);
            }

            return Ok(new RequestResult { reason = ErrorReasons.LanguageNotSupported, result = false });
        }
    }

    [ApiController]
    [Route("Dictionary/[controller]")]
    public class ParametersController : Controller
    {
        class ParametersResult: RequestResult
        {
            public int length { get; set; }
        }

        [HttpGet(Name = "Parameters")]
        public IActionResult GetParameters([FromQuery] string language = "hu")
        {
            string[]? words;
            if (language == "hu")
            {
                if (Words.words_array == null)
                {
                    _ = Words.GetWord(); //load db
                }

                words = Words.words_array;
            }
            else
            {
                return Ok(new ParametersResult { length = 0, reason = ErrorReasons.LanguageNotSupported, result = false });
            }

            if (words == null)
            {
                return Ok(new ParametersResult { length = 0, reason = ErrorReasons.LanguageNotFound, result = false });
            }

            return Ok(new ParametersResult { length = words.Length, reason = 0, result = true });
        }
    }

    [ApiController]
    [Route("Dictionary/[controller]")]
    public class ContentController : Controller
    {
        class ContentResult : RequestResult
        {
            public string[]? words { get; set; }
        }

        [HttpGet(Name = "Content")]
        public IActionResult GetContent([FromQuery] int start = 0, [FromQuery] int count = 0, [FromQuery] string language = "hu")
        {
            string[]? words;
            if (language == "hu")
            {
                if (Words.words_array == null)
                {
                    _ = Words.GetWord(); //load db
                }

                words = Words.words_array;
            }
            else
            {
                return Ok(new ContentResult { words = null, reason = ErrorReasons.LanguageNotSupported, result = false });
            }

            if (words == null)
            {
                return Ok(new ContentResult { words = null, reason = ErrorReasons.LanguageNotFound, result = false });
            }

            if (start < 0 || count < 0 || start >= words.Length || (start + count) > words.Length)
            {
                return Ok(new ContentResult { words = null, reason = ErrorReasons.IndexOutOfBounds, result = false });
            }
            if(count > 1000)
            {
                return Ok(new ContentResult { words = null, reason = ErrorReasons.CountOverLimit, result = false });
            }

            return Ok(new ContentResult { words = words[start..(start+count)], reason = 0, result = true });
        }
    }
}
