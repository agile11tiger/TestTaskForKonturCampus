using NUnit.Framework;

namespace GitTask
{
    [TestFixture]
    public class GitTests
    {
        private const int DefaultFilesCount = 50000;
        private Git sut;

        [SetUp]
        public void SetUp()
        {
            sut = new Git(DefaultFilesCount);
        }

        [Test]
        public void YouTried()
        {
            sut.Update(0,5);
            sut.Commit();
            sut.Update(0, 6);
            Assert.AreEqual(5, sut.Checkout(0, 0));
        }

        [Test]
        public void Checkout_NoUpdate_Commit_ReturnsZero()
        {
            sut.Commit();
            Assert.AreEqual(0, sut.Checkout(0, 0));
        }

        [Test]
        public void Time()
        {
            for (var i = 0; i < 1000; i++)
            {
                sut.Update(i, i);
                sut.Commit();
                sut.Checkout(0, 0);
            }
        }
    }
}