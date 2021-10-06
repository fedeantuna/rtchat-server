using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using RTChat.Server.Domain.Common;
using Shouldly;
using Xunit;

namespace RTChat.Server.Domain.UnitTests.Common
{
    [ExcludeFromCodeCoverage]
    public class ValueObjectTests
    {
        [Theory]
        [MemberData(nameof(EqualValueObjects))]
        public void Equals_EqualValueObjects_ReturnsTrue(ValueObject instanceA, ValueObject instanceB, String reason)
        {
            // Act
            var sut = EqualityComparer<ValueObject>.Default.Equals(instanceA, instanceB);

            // Assert
            sut.ShouldBeTrue(reason);
        }

        [Theory]
        [MemberData(nameof(NonEqualValueObjects))]
        public void Equals_NonEqualValueObjects_ReturnsFalse(ValueObject instanceA, ValueObject instanceB, String reason)
        {
            // Act
            var sut = EqualityComparer<ValueObject>.Default.Equals(instanceA, instanceB);

            // Assert
            sut.ShouldBeFalse(reason);
        }

        private static readonly ValueObject APrettyValueObject = new ValueObjectA(1, "2", Guid.Parse("97ea43f0-6fef-4fb7-8c67-9114a7ff6ec0"), new ComplexObject(2, "3"));

        public static readonly TheoryData<ValueObject, ValueObject, String> EqualValueObjects = new()
        {
            {
                null,
                null,
                "they should be equal because they are both null"
            },
            {
                APrettyValueObject,
                APrettyValueObject,
                "they should be equal because they are the same object"
            },
            {
                new ValueObjectA(1, "2", Guid.Parse("97ea43f0-6fef-4fb7-8c67-9114a7ff6ec0"), new ComplexObject(2, "3")),
                new ValueObjectA(1, "2", Guid.Parse("97ea43f0-6fef-4fb7-8c67-9114a7ff6ec0"), new ComplexObject(2, "3")),
                "they should be equal because they have equal members"
            },
            {
                new ValueObjectA(1, "2", Guid.Parse("97ea43f0-6fef-4fb7-8c67-9114a7ff6ec0"), new ComplexObject(2, "3"), "alpha"),
                new ValueObjectA(1, "2", Guid.Parse("97ea43f0-6fef-4fb7-8c67-9114a7ff6ec0"), new ComplexObject(2, "3"), "beta"),
                "they should be equal because all equality components are equal, even though an additional member was set"
            },
            {
                new ValueObjectB(1, "2",  1, 2, 3 ),
                new ValueObjectB(1, "2",  1, 2, 3 ),
                "they should be equal because all equality components are equal, including the 'C' list"
            }
        };

        public static readonly TheoryData<ValueObject, ValueObject, String> NonEqualValueObjects = new()
        {
            {
                new ValueObjectA(1, "2", Guid.Parse("97ea43f0-6fef-4fb7-8c67-9114a7ff6ec0"), new ComplexObject(2, "3")),
                new ValueObjectA(2, "2", Guid.Parse("97ea43f0-6fef-4fb7-8c67-9114a7ff6ec0"), new ComplexObject(2, "3")),
                "they should not be equal because the 'A' member on ValueObjectA is different among them"
            },
            {
                new ValueObjectA(1, "2", Guid.Parse("97ea43f0-6fef-4fb7-8c67-9114a7ff6ec0"), new ComplexObject(2, "3")),
                new ValueObjectA(1, null, Guid.Parse("97ea43f0-6fef-4fb7-8c67-9114a7ff6ec0"), new ComplexObject(2, "3")),
                "they should not be equal because the 'B' member on ValueObjectA is different among them"
            },
            {
                new ValueObjectA(1, "2", Guid.Parse("97ea43f0-6fef-4fb7-8c67-9114a7ff6ec0"), new ComplexObject(2, "3")),
                new ValueObjectA(1, "2", Guid.Parse("97ea43f0-6fef-4fb7-8c67-9114a7ff6ec0"), new ComplexObject(3, "3")),
                "they should not be equal because the 'A' member on ValueObjectA's 'D' member is different among them"
            },
            {
                new ValueObjectA(1, "2", Guid.Parse("97ea43f0-6fef-4fb7-8c67-9114a7ff6ec0"), new ComplexObject(2, "3")),
                new ValueObjectB(1, "2"),
                "they should not be equal because they are not of the same type"
            },
            {
                new ValueObjectB(1, "2",  1, 2, 3 ),
                new ValueObjectB(1, "2",  1, 2, 3, 4 ),
                "they should be not be equal because the 'C' list contains one additional value"
            },
            {
                new ValueObjectB(1, "2",  1, 2, 3, 5 ),
                new ValueObjectB(1, "2",  1, 2, 3 ),
                "they should be not be equal because the 'C' list contains one additional value"
            },
            {
                new ValueObjectB(1, "2",  1, 2, 3, 5 ),
                new ValueObjectB(1, "2",  1, 2, 3, 4 ),
                "they should be not be equal because the 'C' lists are not equal"
            }

        };

        private class ValueObjectA : ValueObject
        {
            public ValueObjectA(Int32 a, String b, Guid c, ComplexObject d, String notAnEqualityComponent = null)
            {
                A = a;
                B = b;
                C = c;
                D = d;
                NotAnEqualityComponent = notAnEqualityComponent;
            }

            private Int32 A { get; }
            private String B { get; }
            private Guid C { get; }
            private ComplexObject D { get; }
            private String NotAnEqualityComponent { get; }

            protected override IEnumerable<Object> GetEqualityComponents()
            {
                yield return A;
                yield return B;
                yield return C;
                yield return D;
            }
        }

        private class ValueObjectB : ValueObject
        {
            public ValueObjectB(Int32 a, String b, params Int32[] c)
            {
                A = a;
                B = b;
                C = c.ToList();
            }

            private Int32 A { get; }
            private String B { get; }
            private List<Int32> C { get; }

            protected override IEnumerable<Object> GetEqualityComponents()
            {
                yield return A;
                yield return B;

                foreach (var c in C)
                {
                    yield return c;
                }
            }
        }

        private class ComplexObject : IEquatable<ComplexObject>
        {
            public ComplexObject(Int32 a, String b)
            {
                A = a;
                B = b;
            }

            private Int32 A { get; }

            private String B { get; }

            public override Boolean Equals(Object obj)
            {
                return Equals(obj as ComplexObject);
            }

            public Boolean Equals(ComplexObject other)
            {
                return other != null &&
                       A == other.A &&
                       B == other.B;
            }

            public override Int32 GetHashCode()
            {
                return HashCode.Combine(A, B);
            }
        }
    }
}