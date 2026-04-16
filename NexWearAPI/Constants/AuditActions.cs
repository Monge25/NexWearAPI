namespace NexWearAPI.Constants
{
    public static class AuditActions
    {
        // Auth
        public const string LOGIN_SUCCESS = "LOGIN_SUCCESS";
        public const string LOGIN_FAILED = "LOGIN_FAILED";
        public const string REGISTER = "REGISTER";
        public const string LOGOUT = "LOGOUT";
        public const string PASSWORD_RESET_REQUEST = "PASSWORD_RESET_REQUEST";
        public const string PASSWORD_RESET_SUCCESS = "PASSWORD_RESET_SUCCESS";

        // Orders
        public const string ORDER_CREATED = "ORDER_CREATED";
        public const string ORDER_STATUS_CHANGED = "ORDER_STATUS_CHANGED";
        public const string ORDER_CANCELLED = "ORDER_CANCELLED";

        // Cart
        public const string CART_ITEM_ADDED = "CART_ITEM_ADDED";
        public const string CART_ITEM_REMOVED = "CART_ITEM_REMOVED";
        public const string CART_CLEARED = "CART_CLEARED";

        // Admin
        public const string ADMIN_USER_ROLE_CHANGED = "ADMIN_USER_ROLE_CHANGED";
        public const string ADMIN_REVIEW_MODERATED = "ADMIN_REVIEW_MODERATED";
        public const string ADMIN_ORDER_STATUS_CHANGED = "ADMIN_ORDER_STATUS_CHANGED";

        // Products
        public const string PRODUCT_CREATED = "PRODUCT_CREATED";
        public const string PRODUCT_DELETED = "PRODUCT_DELETED";
        public const string PRODUCT_UPDATED = "PRODUCT_UPDATED";

        // Reviews
        public const string REVIEW_CREATED = "REVIEW_CREATED";
        public const string REVIEW_DELETED = "REVIEW_DELETED";
    }

    public static class AuditCategories
    {
        public const string Auth = "Auth";
        public const string Order = "Order";
        public const string Cart = "Cart";
        public const string Admin = "Admin";
        public const string Product = "Product";
        public const string Review = "Review";
    }
}
