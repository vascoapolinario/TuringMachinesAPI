using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using System.Threading.Tasks;
using TuringMachinesAPI.Dtos;
using TuringMachinesAPI.Entities;
using TuringMachinesAPI.Enums;
using TuringMachinesAPI.Services;

namespace TuringMachinesAPI.Controllers
{
    [ApiController]
    [Route("community")]
    public class CommunityController : ControllerBase
    {
        private readonly CommunityService communityService;
        private readonly DiscordWebhookService discordWebhookService;
        private readonly AdminLogService adminLogService;

        public CommunityController(CommunityService communityService, DiscordWebhookService _discordwebHook, AdminLogService adminLogService)
        {
            this.communityService = communityService;
            this.discordWebhookService = _discordwebHook;
            this.adminLogService = adminLogService;
        }

        /// <summary>
        /// Get all discussions.
        /// </summary>
        /// <returns>A list of discussions.</returns>
        [HttpGet("discussions")]
        [Authorize]
        [ProducesResponseType(typeof(IEnumerable<Dtos.Discussion>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        public IActionResult GetDiscussions()
        {
            var discussions = communityService.GetDiscussions();
            return Ok(discussions);
        }

        /// <summary>
        /// Get a discussion by ID.
        /// </summary>
        /// <param name="id">The ID of the discussion.</param>
        [HttpGet("discussions/{id:int}")]
        [Authorize]
        [ProducesResponseType(typeof(Dtos.Discussion), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        public IActionResult GetDiscussionById(int id)
        {
            var discussion = communityService.GetDiscussionById(id);
            if (discussion is null)
                return NotFound($"Discussion with ID {id} not found.");
            return Ok(discussion);
        }


        /// <summary>
        /// Create a new discussion.
        /// </summary>
        /// <param name="title">The title of the discussion.</param>
        /// <param name="content">The content of the initial post.</param>
        /// <param name="category">The category of the discussion.</param>
        /// <returns>The created discussion.</returns>
        [HttpPost("discussions")]
        [Authorize]
        [ProducesResponseType(typeof(Dtos.Discussion), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        public async Task<IActionResult> CreateDiscussion([FromForm] string title, [FromForm] string content, [FromForm] string category)
        {
            int authorId = int.Parse(User.FindFirst("id")!.Value);
            var discussion = communityService.CreateDiscussion(title, authorId, content, category);
            if (discussion is null)
                return BadRequest("Failed to create discussion. Due to invalid input.");
            
            await adminLogService.CreateAdminLog(authorId, Enums.ActionType.Create, Enums.TargetEntityType.Discussion, discussion.Id);
            await discordWebhookService.NotifyNewDiscussionAsync(discussion.Title, discussion.AuthorName);
            return CreatedAtAction(nameof(GetDiscussionById), new { id = discussion.Id }, discussion);
        }

        /// <summary>
        /// Post to a discussion.
        /// </summary>
        /// <param name="discussionId">The ID of the discussion.</param>
        /// <param name="content">The content of the post.</param>
        /// <returns>The created post.</returns>
        [HttpPost("discussions/{discussionId:int}/post")]
        [Authorize]
        [ProducesResponseType(typeof(Dtos.Post), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        public async Task<IActionResult> PostToDiscussion(int discussionId, [FromForm] string content)
        {
            int authorId = int.Parse(User.FindFirst("id")!.Value);
            var post = communityService.CreatePost(discussionId, authorId, content);
            if (post is null)
                return BadRequest("Failed to post to discussion. Due to invalid input.");

            await adminLogService.CreateAdminLog(authorId, Enums.ActionType.Create, Enums.TargetEntityType.Post, post.Id);
            var discussionTitle = communityService.GetDiscussionById(discussionId)?.Title ?? "a discussion";
            await discordWebhookService.NotifyNewDiscussionPostAsync(discussionTitle, post.AuthorName);
            return CreatedAtAction(nameof(GetDiscussionById), new { id = discussionId }, post);
        }

        /// <summary>
        /// Edit a post.
        /// </summary>
        /// <param name="postId">The ID of the post.</param>
        /// <param name="content">The new content of the post.</param>
        /// <returns>The updated post.</returns>
        [HttpPut("posts/{postId:int}")]
        [Authorize]
        [ProducesResponseType(typeof(Dtos.Post), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        public async Task<IActionResult> EditPost(int postId, [FromForm] string content)
        {
            int editorId = int.Parse(User.FindFirst("id")!.Value);
            var post = communityService.EditPost(postId, editorId, content);
            if (post is null)
                return BadRequest("Failed to edit post. Due to invalid input.");
            
            await adminLogService.CreateAdminLog(editorId, Enums.ActionType.Update, Enums.TargetEntityType.Post, post.Id);
            return Ok(post);
        }

        /// <summary>
        /// Delete a post.
        /// </summary>
        /// <param name="postId">The ID of the post.</param>
        /// <returns>Bool indicating success or failure.</returns>
        [HttpDelete("posts/{postId:int}")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        public async Task<IActionResult> DeletePost(int postId)
        {
            int authorId = int.Parse(User.FindFirst("id")!.Value);
            bool deleted = communityService.DeletePost(postId, authorId);
            if (!deleted)
                return NotFound("Post not found.");

            await adminLogService.CreateAdminLog(authorId, Enums.ActionType.Delete, Enums.TargetEntityType.Post, postId);
            return NoContent();
        }

        /// <summary>
        /// Toggle discussion closed status.
        /// </summary>
        /// <param name="discussionId">The ID of the discussion.</param>
        /// <returns>Bool indicating success or failure.</returns>
        [HttpPost("discussions/{discussionId:int}/toggle-closed")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        public IActionResult ToggleDiscussionClosed(int discussionId)
        {
            int userId = int.Parse(User.FindFirst("id")!.Value);
            bool toggled = communityService.ToggleCloseDiscussion(discussionId, userId);
            if (!toggled)
                return NotFound("Discussion not found.");
            return Ok();
        }

        /// <summary>
        /// Choose/Toggle an answer post for a discussion.
        /// </summary>
        /// <param name="discussionId">The ID of the discussion.</param>
        /// <param name="postId">The ID of the post to set as answer.</param>
        /// <returns>Bool indicating success or failure.</returns>
        [HttpPost("discussions/{discussionId:int}/{postId:int}/choose-answer")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        public IActionResult ChooseAnswer(int discussionId, int postId)
        {
            int userId = int.Parse(User.FindFirst("id")!.Value);
            bool chosen = communityService.ToggleDiscussionAnswerPost(discussionId, postId, userId);
            if (!chosen)
                return NotFound("Discussion or post not found.");
            return Ok();
        }

        /// <summary>
        /// Toggle like on a post
        /// </summary>
        /// <param name="postId">The ID of the post.</param>
        /// <returns>Bool indicating success or failure.</returns>
        [HttpPost("posts/{postId:int}/like")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        public IActionResult ToggleLikePost(int postId)
        {
            int userId = int.Parse(User.FindFirst("id")!.Value);
            bool toggled = communityService.ToggleLikePost(postId, userId);
            if (!toggled)
                return NotFound("Post not found.");
            return Ok();
        }

        /// <summary>
        /// Toggle a dislike on a post
        /// </summary>
        /// <param name="postId">The ID of the post.</param>
        /// <returns>Bool indicating success or failure.</returns>
        [HttpPost("posts/{postId:int}/dislike")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        public IActionResult ToggleDislikePost(int postId)
        {
            int userId = int.Parse(User.FindFirst("id")!.Value);
            bool toggled = communityService.ToggleDislikePost(postId, userId);
            if (!toggled)
                return NotFound("Post not found.");
            return Ok();
        }

        /// <summary>
        /// Get posts by discussion ID.
        /// </summary>
        /// <param name="discussionId">The ID of the discussion.</param>
        /// <param name="includeUserVote">Whether to include the user's vote on each post.</param>"
        /// incase includeUserVote is true, returns a list of tuples containing the post and the user's vote (1 for like, -1 for dislike, null for no vote). If false, returns a list of posts only.
        [HttpGet("discussions/{discussionId:int}/posts")]
        [Authorize]
        [ProducesResponseType(typeof(IEnumerable<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        public IActionResult GetPostsByDiscussionId(int discussionId, [FromQuery] bool includeUserVote = false)
        {
            int userId = int.Parse(User.FindFirst("id")!.Value);
            if (includeUserVote)
            {
                var postsWithVotes = communityService.GetPostsByDiscussionIdWithUserVote(discussionId, userId);
                if (postsWithVotes is null)
                    return NotFound("Discussion not found.");
                return Ok(postsWithVotes.Select(pv => new
                {
                    post = pv.post,
                    userVote = pv.userVote
                }));
            }
            else
            {
                var posts = communityService.GetPostsByDiscussionId(discussionId);
                if (posts is null)
                    return NotFound("Discussion not found.");
                return Ok(posts);
            }
        }

        /// <summary>
        /// Delete a discussion by ID.
        /// </summary>
        /// <param name="discussionId">The ID of the discussion.</param>
        /// <returns>Bool indicating success or failure.</returns>
        [HttpDelete("discussions/{discussionId:int}")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        public async Task<IActionResult> DeleteDiscussion(int discussionId)
        {
            int userId = int.Parse(User.FindFirst("id")!.Value);
            bool deleted = communityService.DeleteDiscussion(discussionId, userId);
            if (!deleted)
                return NotFound("Discussion not found.");

            await adminLogService.CreateAdminLog(userId, Enums.ActionType.Delete, Enums.TargetEntityType.Post, discussionId);
            return Ok();
        }
    }
}