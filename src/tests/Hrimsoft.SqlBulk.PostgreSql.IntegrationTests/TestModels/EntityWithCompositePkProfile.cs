namespace Hrimsoft.SqlBulk.PostgreSql.IntegrationTests.TestModels
{
    public class EntityWithCompositePkProfile : EntityProfile
    {
        public EntityWithCompositePkProfile(int maximumSentElements = 0)
            : base(typeof(TestEntityWithCompositePk))
        {
            this.MaximumSentElements = maximumSentElements;

            this.ToTable("entity_with_composite_pk", "unit_tests");
            this.HasUniqueConstraint("PK_entity_with_composite_pk");
            this.HasPropertyAsPartOfUniqueConstraint<TestEntityWithCompositePk, int>(entity => entity.UserId)
                .ThatIsPrivateKey();
            this.HasPropertyAsPartOfUniqueConstraint<TestEntityWithCompositePk, int>(entity => entity.Column2)
                .ThatIsPrivateKey();
            this.HasProperty<TestEntityWithCompositePk, int>(entity => entity.Column3);
        }
    }
}