using Xunit;
using Passwords.API.Abstracts;

namespace Passwords.Tests
{
    public class BaseClasses
    {
        private readonly Status CriticalCaseOne = new Status(ResultCode.Invalid | ResultCode.Success);
        private readonly Status CriticalCaseTwo = new Status(ResultCode.Unknown | ResultCode.Success);

        public BaseClasses()
        { }

        public void AssertNoStateCase(Status nostate)
        {
            Assert.False(nostate, nostate.ToString()); // if nothing there IS, than nothing could pass
            Assert.False(nostate.Bad, nostate); // if nothing there 'is', then there's nothing Bad
            Assert.False(nostate.Ok, nostate); // if nothing there IS, for what is it Good for?
            Assert.False(nostate.Intermediate, nostate); // as long nothing has been initiated, nothing awaited will happen (e.g. no cause - no effect)
            Assert.True(nostate.ToString().StartsWith("Status"), nostate);
        }

        public void AssertSuccessCase(Status success)
        {
            Assert.True(success, success ); // passes silently, sure
            Assert.False(success.Bad, success); // no, it's ok
            Assert.True(success.Ok, success); // yes that it is
            Assert.False(success.Intermediate, success); // for what at all?
            Assert.True(success.ToString().StartsWith("Success"),success);
        }

        public void AssertUnknownCase(Status unknown)
        {
            Assert.True(unknown, unknown.ToString()); // as long there aren't explicit questions about it..
            Assert.True(unknown.Bad, unknown); // I don't know if it's ok,... so could be maybe Bad
            Assert.False(unknown.Ok, unknown); // one cannot say it would be ok for sure yet..
            Assert.True(unknown.Intermediate, unknown); // not yet evaluable, so wait till gets known
            Assert.True(unknown.ToString().StartsWith("Status"), unknown);
        }

        public void AssertInvalidCase(Status invalid)
        {
            Assert.False(invalid, invalid ); // should not pass silently
            Assert.True(invalid.Bad, invalid); // is 'Bad' in any case
            Assert.False(invalid.Ok, invalid); // is NOT Ok at all
            Assert.False(invalid.Intermediate, invalid); // No, it's already evaluated being 'Invalid'
            Assert.True(invalid.ToString().StartsWith("Error"), invalid );
        }

        [Fact]
        public void BasicStatusValues()
        {
            AssertNoStateCase(Status.NoState);
            AssertSuccessCase(Status.Success);
            AssertUnknownCase(Status.Unknown);
            AssertInvalidCase(Status.Invalid);
        }

        [Fact]
        public void StatusValueOperations()
        {
            // Assert that 'Invalid' Status won't become successive
            AssertInvalidCase(Status.Invalid + ResultCode.Success);

            // Assert an 'Unknown' Status later may change
            AssertSuccessCase(Status.Unknown + Status.Success);

            // Assert raising Status.Code via appending Status or ResultCode changes results  
            AssertUnknownCase(Status.NoState + ResultCode.Unknown);
            AssertUnknownCase(Status.NoState + Status.Unknown);

            // Asserting weather appending message Text nor info Data may change a result 
            AssertNoStateCase(Status.NoState.WithText("State of the Art"));
            AssertNoStateCase(Status.NoState.WithData("A fistful of Data"));

            // Assert status 'Unknown' for the status of context related item data   
            Status Chicken  = Status.NoState.WithText("Chicken or").WithData("Egg") + Status.Unknown;
            AssertUnknownCase( Chicken );
            Assert.True( Chicken.Text.EndsWith("Chicken or Egg"), Chicken.ToString() );

            // Asserting correct behavior on status transition to a different context  
            Status Banana = Chicken.WithText("Banana is") + ResultCode.Info + Status.Invalid;
            AssertInvalidCase( Banana );
            Assert.True( Banana.ToString().EndsWith("Egg"), Banana.ToString() );
            Assert.False( Banana.Text.StartsWith("Chicken"), Banana.ToString() ); 
            Assert.True( Banana.Text.Contains("Banana"), Banana.ToString() );

            // Appending descriptive data to a status af transitioned contexts  
            Banana = Chicken.WithText("Banana is") + Status.Success.WithData("Gelb");
            AssertSuccessCase( Banana );
            Assert.False(Banana.Text.StartsWith("Chicken"), Banana.Text);
            Assert.True(Banana.Text.EndsWith("Gelb"), Banana.Text);
        }
    }
}
