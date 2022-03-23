using Xunit;
using PasswordsAPI.Abstracts;

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

        public void AssertNoStateCase(Status nostate)
        {
            Assert.False(nostate, nostate.ToString()); // if nothing there IS, than nothing could pass
            Assert.False(nostate.Bad, nostate); // if nothing there IS, then there's nothing Bad
            Assert.False(nostate.Ok, nostate); // if nothing there IS, for what is it Good for?
            Assert.False(nostate.Waiting, nostate); // as long nothing has been initiated, nothing awaited will happen (e.g. no cause - no effect)
            Assert.True(nostate.ToString().StartsWith("Status"), nostate.ToString());
        }

        public void AssertSuccessCase(Status success)
        {
            Assert.True(success, success.ToString()); // passes silently, sure
            Assert.False(success.Bad, success); // no, it's ok
            Assert.True(success.Ok, success); // yes that it is
            Assert.False(success.Waiting, success); // for what at all?
            Assert.True(success.ToString().StartsWith("Success"),success.ToString());
        }

        public void AssertUnknownCase(Status unknown)
        {
            Assert.True(unknown, unknown.ToString()); // as long there aren't explicit questions about it..
            Assert.True(unknown.Bad, unknown); // I don't know if it's ok,... so could be maybe Bad
            Assert.False(unknown.Ok, unknown); // one cannot say it would be ok for sure yet..
            Assert.True(unknown.Waiting, unknown); // not yet evaluable, so wait till gets known
            Assert.True(unknown.ToString().StartsWith("Status"), unknown.ToString());
        }

        public void AssertInvalidCase(Status invalid)
        {
            Assert.False(invalid, invalid.ToString()); // should not pass silently
            Assert.True(invalid.Bad, invalid); // is 'Bad' in any case
            Assert.False(invalid.Ok, invalid); // is NOT Ok at all
            Assert.False(invalid.Waiting, invalid); // No, it's already evaluated being 'Invalid'
            Assert.True(invalid.ToString().StartsWith("Error"),invalid.ToString());
        }

        [Fact]
        public void BasicStatusValues()
        {
            AssertNoStateCase(NoState);
            AssertSuccessCase(Success);
            AssertUnknownCase(Unknown);
            AssertInvalidCase(Invalid);
        }

        [Fact]
        public void StatusValueOperations()
        {
            AssertInvalidCase(Invalid + ResultCode.Success);

            AssertSuccessCase(Unknown + Success);

            AssertUnknownCase(NoState + ResultCode.Unknown);
            AssertUnknownCase(NoState + Unknown);

            AssertNoStateCase(NoState.WithText("State of the Art"));
            AssertNoStateCase(NoState.WithData("A fistful of Data"));

            Status Chicken  = NoState.WithText("Chicken or").WithData("Egg") + Unknown;
            AssertUnknownCase( Chicken );
            Assert.True( Chicken.ToString().EndsWith("Chicken or Egg"), Chicken );
            

            Status Banana = Chicken.WithText("Banana is") + ResultCode.Info + Invalid;
            AssertInvalidCase( Banana );
            Assert.False( !Banana.ToString().EndsWith("Egg"), Banana.ToString() );
            Assert.False( Banana.Text.StartsWith("Banana"), Banana.ToString() ); 
            Assert.True( Banana.Text.Contains("Banana"), Banana.ToString() );
            Banana = Chicken.WithText("Banana is") + Success.WithData("Gelb");
            AssertSuccessCase( Banana );
            Assert.False(Banana.Text.StartsWith("Chicken"), Banana.ToString());
            Assert.True(Banana.Data.ToString().Equals("Gelb"), Banana.ToString());
        }
    }
}
