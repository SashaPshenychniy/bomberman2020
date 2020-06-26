using log4net.Config;
using NUnit.Framework;

namespace Bomberman.Api.Tests
{
    [TestFixture]
    public class MyTests
    {
        [OneTimeSetUp]
        public void Init()
        {
            XmlConfigurator.Configure();
        }

        [Test]
        public void Test1()
        {
            var board = @"
☼☼☼☼☼☼☼☼☼☼☼☼☼☼☼☼☼☼☼☼☼☼☼
☼☺  3#           #   #☼
☼ ☼ ☼♥☼ ☼ ☼ ☼ ☼ ☼ ☼ ☼#☼
☼&  +   &  #  #  # #  ☼
☼ ☼ ☼ ☼ ☼ ☼ ☼ ☼ ☼ ☼ ☼ ☼
☼#c  #         r    ##☼
☼ ☼♠☼ ☼#☼i☼ ☼ ☼ ☼ ☼#☼ ☼
☼  #   #             #☼
☼ ☼ ☼ ☼ ☼#☼ ☼#☼#☼ ☼ ☼ ☼
☼#         ##      # #☼
☼ ☼ ☼ ☼ ☼ ☼#☼ ☼ ☼ ☼#☼ ☼
☼     #   ♥    &      ☼
☼ ☼ ☼ ☼ ☼#☼ ☼ ☼ ☼ ☼ ☼ ☼
☼               #     ☼
☼ ☼ ☼ ☼ ☼ ☼ ☼ ☼ ☼ ☼ ☼ ☼
☼       #        #   #☼
☼ ☼ ☼ ☼ ☼ ☼#☼#☼ ☼ ☼ ☼ ☼
☼## #   #          #  ☼
☼ ☼ ☼ ☼#☼ ☼ ☼ ☼ ☼ ☼ ☼ ☼
☼&#       #   # #   ♥ ☼
☼#☼#☼#☼ ☼ ☼ ☼#☼ ☼ ☼ ☼ ☼
☼#♥            &     #☼
☼☼☼☼☼☼☼☼☼☼☼☼☼☼☼☼☼☼☼☼☼☼☼";

            var s = new MySolver("");
            var action = s.GetAction(board.Replace("\n", "").Replace("\r", ""));
            
            Assert.AreEqual("Act, Right", action);
        }
    }
}
