namespace DevProject.Tests
{
    using Moq.AutoMock;

    public abstract class TestBase<TSut> where TSut : class
    {
        protected readonly AutoMocker automocker = new AutoMocker();

        protected TSut CreateTestSubject()
            => automocker.CreateInstance<TSut>();
    }
}