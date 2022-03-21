using Xunit;
using PasswordsAPI.BaseClasses;

namespace PasswordsAPI.Tests
{
    public class BaseClasses
    {
        private readonly Status Success = Status.Success;
        private readonly Status NoState = Status.NoError;
        private readonly Status Invalid = Status.Invalid;
        private readonly Status Unknown = Status.Unknown;
        private readonly Status Cryptic = Status.Cryptic;
        private readonly Status Service = Status.Service;

        private readonly Status CriticalCaseOne = new Status(ResultCode.Invalid | ResultCode.Success);
        private readonly Status CriticalCaseTwo = new Status(ResultCode.Unknown | ResultCode.Success);

        public BaseClasses()
        { }

        [Fact]
        public void CriticalStatus()
        {
            Assert.False(NoState); // if nothing there IS, than nothing could pass
            Assert.False(NoState.Bad); // if nothing there IS, then there's nothing Bad
            Assert.False(NoState.Ok); // if nothing there IS, for what is it Good for?
            Assert.False(NoState.Waiting); // as long nothing has been initiated, nothing awaited will happen (e.g. no cause - no effect) 

            Assert.True(Success); // passes silently, sure
            Assert.False(Success.Bad); // no, it's ok
            Assert.True(Success.Ok); // yes that it is
            Assert.False(Success.Waiting); // for what at all?

            Assert.True(Unknown); // as long there aren't explicit questions about it..
            Assert.True(Unknown.Bad); // I don't know if it's ok,... so could be maybe Bad
            Assert.False(Unknown.Ok); // as above,... one cannot say it would be ok for sure
            Assert.True(Unknown.Waiting); // seems not been evaluated yet, ...but maybe later its learned to known

            Assert.False(Invalid); // should not pass silently
            Assert.True(Invalid.Bad); // is 'Bad' in any case
            Assert.False(Invalid.Ok); // is NOT Ok at all
            Assert.False(Invalid.Waiting); // No, it's already evaluated being 'Invalid'
        }
    }
}
