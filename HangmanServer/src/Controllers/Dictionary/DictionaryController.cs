using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Formatters;

namespace HangmanServer.src.Controllers.Dictionary
{
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
                return Ok(new ParametersResult { length = 0, message = "Language not supported!", result = false });
            }

            if (words == null)
            {
                return Ok(new ParametersResult { length = 0, message = "Language dictionary not found!", result = false });
            }

            return Ok(new ParametersResult { length = words.Length, message = "", result = true });
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
                return Ok(new ContentResult { words = null, message = "Language not supported!", result = false });
            }

            if (words == null)
            {
                return Ok(new ContentResult { words = null, message = "Language dictionary not found!", result = false });
            }

            if (start < 0 || count < 0 || start >= words.Length || (start + count) > words.Length)
            {
                return Ok(new ContentResult { words = null, message = "Indices out of bounds!", result = false });
            }
            if(count > 1000)
            {
                return Ok(new ContentResult { words = null, message = "Count is larger than 1000!", result = false });
            }

            return Ok(new ContentResult { words = words[start..(start+count)], message = "", result = true });
        }
    }
}
