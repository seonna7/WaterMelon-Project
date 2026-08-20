namespace Game
{
    namespace Action
    {
        public struct ActionResult
        {
            public bool Success;

            public ActionFailReason FailReason;

            public int UsedGem;

            public static ActionResult CreateSuccess(int usedGem)
            {
                return new ActionResult
                {
                    Success = true,
                    FailReason = ActionFailReason.None,
                    UsedGem = usedGem
                };
            }

            public static ActionResult CreateFail(ActionFailReason failReason)
            {
                return new ActionResult
                {
                    Success = false,
                    FailReason = failReason,
                    UsedGem = 0
                };
            }
        }
    }
}
