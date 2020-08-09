﻿using AutoFixture;
using AutoFixture.AutoNSubstitute;
using AutoFixture.Idioms;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TheInternet.Common.SessionManagement;

namespace TheInternet.Common.UnitTests.SessionManagement
{
    [TestClass]
    public class DriverSessionTests
    {
        [TestMethod]
        public void All_Methods_Have_Guards()
        {
            // Arrange
            var fixture = new Fixture();

            fixture.Customize(new AutoNSubstituteCustomization());

            var assertThatAllMembersHaveGuards = new GuardClauseAssertion(fixture);

            // Act, Assert
            assertThatAllMembersHaveGuards.Verify(typeof(DriverSession).GetMembers());
        }
    }
}
