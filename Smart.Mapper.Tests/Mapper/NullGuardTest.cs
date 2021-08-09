namespace Smart.Mapper
{
    using Xunit;

    public class NullGuardTest
    {
        //--------------------------------------------------------------------------------
        // Class
        //--------------------------------------------------------------------------------

        [Fact]
        public void GuardForAction()
        {
            using var mapper = new MapperConfig()
                .AddDefaultMapper()
                .ToMapper();

            var destination = new Destination();
            mapper.Map<Source, Destination>(null!, destination);
        }

        [Fact]
        public void GuardForFunc()
        {
            using var mapper = new MapperConfig()
                .AddDefaultMapper()
                .ToMapper();

            var destination = mapper.Map<Source, Destination>(null!);

            Assert.Null(destination);
        }

        [Fact]
        public void GuardForParameterAction()
        {
            using var mapper = new MapperConfig()
                .AddDefaultMapper()
                .ToMapper();

            var destination = new Destination();
            mapper.Map<Source, Destination>(null!, destination, 0);
        }

        [Fact]
        public void GuardForParameterFunc()
        {
            using var mapper = new MapperConfig()
                .AddDefaultMapper()
                .ToMapper();

            var destination = mapper.Map<Source, Destination>(null!, 0);

            Assert.Null(destination);
        }

        //--------------------------------------------------------------------------------
        // Nullable
        //--------------------------------------------------------------------------------

        [Fact]
        public void GuardForNullableAction()
        {
            using var mapper = new MapperConfig()
                .AddDefaultMapper()
                .ToMapper();

            var destination = default(StructDestination);
            mapper.Map<StructSource?, StructDestination?>(null!, destination);
        }

        [Fact]
        public void GuardForNullableFunc()
        {
            using var mapper = new MapperConfig()
                .AddDefaultMapper()
                .ToMapper();

            var destination = mapper.Map<StructSource?, StructDestination?>(null!);

            Assert.Null(destination);
        }

        [Fact]
        public void GuardForNullableParameterAction()
        {
            using var mapper = new MapperConfig()
                .AddDefaultMapper()
                .ToMapper();

            var destination = default(StructDestination);
            mapper.Map<StructSource?, StructDestination?>(null!, destination, 0);
        }

        [Fact]
        public void GuardForNullableParameterFunc()
        {
            using var mapper = new MapperConfig()
                .AddDefaultMapper()
                .ToMapper();

            var destination = mapper.Map<StructSource?, StructDestination?>(null!, 0);

            Assert.Null(destination);
        }

        //--------------------------------------------------------------------------------
        // Data
        //--------------------------------------------------------------------------------

        public class Source
        {
        }

        public class Destination
        {
        }

        public struct StructSource
        {
        }

        public struct StructDestination
        {
        }
    }
}
