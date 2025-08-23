namespace SensibleGovernment.DataLayer;

public static class StoredProcedures
{
    // Post procedures
    public const string GetAllPosts = "posts_GetAll";
    public const string GetPostById = "posts_GetById";
    public const string GetPostsByTopic = "posts_GetByTopic";
    public const string CreatePost = "posts_Create";
    public const string UpdatePost = "posts_Update";
    public const string DeletePost = "posts_Delete";
    public const string IncrementPostViewCount = "posts_IncrementViewCount";

    // Comment procedures
    public const string GetCommentsByPostId = "comments_GetByPostId";
    public const string CreateComment = "comments_Create";
    public const string DeleteComment = "comments_Delete";

    // Like procedures
    public const string GetLikesByPostId = "likes_GetByPostId";
    public const string ToggleLike = "likes_Toggle";
    public const string CheckUserLikedPost = "likes_CheckUserLiked";

    // User procedures
    public const string GetAllUsers = "users_GetAll";
    public const string GetUserById = "users_GetById";
    public const string GetUserByEmail = "users_GetByEmail";
    public const string CreateUser = "users_Create";
    public const string UpdateUser = "users_Update";
    public const string UpdateUserPassword = "users_UpdatePassword";
    public const string ToggleUserStatus = "users_ToggleStatus";
    public const string ValidateUserLogin = "users_ValidateLogin";
    public const string UpdateUserLoginInfo = "users_UpdateLoginInfo";
    public const string UpdateUserFailedLogin = "users_UpdateFailedLogin";

    // Admin procedures
    public const string GetDashboardStats = "admin_GetDashboardStats";
    public const string GetRecentComments = "admin_GetRecentComments";

    // Report procedures
    public const string GetPendingReports = "reports_GetPending";
    public const string CreateReport = "reports_Create";
    public const string ResolveReport = "reports_Resolve";

    // PostSource procedures
    public const string GetSourcesByPostId = "postSources_GetByPostId";
    public const string CreatePostSource = "postSources_Create";
    public const string DeletePostSources = "postSources_DeleteByPostId";
}