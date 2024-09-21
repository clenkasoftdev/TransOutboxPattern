using System.ComponentModel.DataAnnotations.Schema;

namespace Clenka.PostManager.Entities
{
    public class User
    {
        //[DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int ID { get; set; }
        public int UserID { get; set; }
        public string Name { get; set; }
    }
}
