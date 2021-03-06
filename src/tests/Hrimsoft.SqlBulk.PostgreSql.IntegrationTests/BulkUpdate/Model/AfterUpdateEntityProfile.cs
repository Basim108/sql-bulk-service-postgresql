using System.Data;

namespace Hrimsoft.SqlBulk.PostgreSql.IntegrationTests.BulkUpdate.Model
{
    /// <summary>
    /// Entity profile that defines properties that have to be included into the returning clause
    /// </summary>
    public class AfterUpdateEntityProfile: EntityProfile
    {
        public AfterUpdateEntityProfile()
            :base(typeof(AfterUpdateEntity))
        {
            this.ToTable("after_update_tests", "unit_tests");

            this.HasProperty<AfterUpdateEntity, int>(entity => entity.Id)
                .ThatIsAutoGenerated()
                .ThatIsPrivateKey();
            this.HasProperty<AfterUpdateEntity, string>(entity => entity.Record)
                .MustBeUpdatedAfterUpdate();
            this.HasProperty<AfterUpdateEntity, string>(entity => entity.Sensor);
            this.HasProperty<AfterUpdateEntity, int>(entity => entity.Value)
                .MustBeUpdatedAfterUpdate();
        }
    }
}