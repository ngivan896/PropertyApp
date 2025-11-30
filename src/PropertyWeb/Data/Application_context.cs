using Microsoft.EntityFrameworkCore;
using PropertyWeb.Models;

namespace PropertyWeb.Data;

public class Application_context : DbContext
{
    public Application_context(DbContextOptions<Application_context> options) : base(options)
    {
    }

    public DbSet<User_account> User_set => Set<User_account>();
    public DbSet<Property_record> Property_set => Set<Property_record>();
    public DbSet<Repair_ticket> Repair_ticket_set => Set<Repair_ticket>();
    public DbSet<Payment_record> Payment_record_set => Set<Payment_record>();
    public DbSet<Ticket_message> Ticket_message_set => Set<Ticket_message>();

    protected override void OnModelCreating(ModelBuilder model_builder)
    {
        model_builder.Entity<User_account>(entity =>
        {
            entity.ToTable("users");
            entity.Property(item => item.Id).HasColumnName("id");
            entity.Property(item => item.User_name).HasColumnName("user_name");
            entity.Property(item => item.Email).HasColumnName("email");
            entity.Property(item => item.Password_hash).HasColumnName("password_hash");
            entity.Property(item => item.Role).HasColumnName("role");
            entity.Property(item => item.Phone).HasColumnName("phone");
            entity.Property(item => item.Created_at).HasColumnName("created_at");
        });

        model_builder.Entity<Property_record>(entity =>
        {
            entity.ToTable("properties");
            entity.Property(item => item.Id).HasColumnName("id");
            entity.Property(item => item.Owner_id).HasColumnName("owner_id");
            entity.Property(item => item.Address_line).HasColumnName("address_line");
            entity.Property(item => item.Unit_label).HasColumnName("unit_label");
            entity.Property(item => item.Property_type).HasColumnName("property_type");
            entity.Property(item => item.Created_at).HasColumnName("created_at");

            entity.HasOne(item => item.Owner)
                .WithMany(owner => owner.Property_list)
                .HasForeignKey(item => item.Owner_id);
        });

        model_builder.Entity<Repair_ticket>(entity =>
        {
            entity.ToTable("repair_tickets");
            entity.Property(item => item.Id).HasColumnName("id");
            entity.Property(item => item.Title).HasColumnName("title");
            entity.Property(item => item.Description).HasColumnName("description");
            entity.Property(item => item.Status).HasColumnName("status").HasConversion<string>();
            entity.Property(item => item.Image_url).HasColumnName("image_url");
            entity.Property(item => item.Created_at).HasColumnName("created_at");
            entity.Property(item => item.Updated_at).HasColumnName("updated_at");
            entity.Property(item => item.Owner_id).HasColumnName("owner_id");
            entity.Property(item => item.Assigned_user_id).HasColumnName("assigned_user_id");
            entity.Property(item => item.Property_id).HasColumnName("property_id");

            entity.HasOne(item => item.Owner)
                .WithMany(owner => owner.Submitted_ticket_list)
                .HasForeignKey(item => item.Owner_id)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(item => item.Assigned_user)
                .WithMany(user => user.Assigned_ticket_list)
                .HasForeignKey(item => item.Assigned_user_id)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(item => item.Property)
                .WithMany(property => property.Ticket_list)
                .HasForeignKey(item => item.Property_id);
        });

        model_builder.Entity<Payment_record>(entity =>
        {
            entity.ToTable("payment_records");
            entity.Property(item => item.Id).HasColumnName("id");
            entity.Property(item => item.Property_id).HasColumnName("property_id");
            entity.Property(item => item.Owner_id).HasColumnName("owner_id");
            entity.Property(item => item.Amount).HasColumnName("amount").HasColumnType("decimal(18,2)");
            entity.Property(item => item.Description).HasColumnName("description");
            entity.Property(item => item.Recorded_at).HasColumnName("recorded_at");

            entity.HasOne(item => item.Property)
                .WithMany(property => property.Payment_list)
                .HasForeignKey(item => item.Property_id);

            entity.HasOne(item => item.Owner)
                .WithMany()
                .HasForeignKey(item => item.Owner_id)
                .OnDelete(DeleteBehavior.Restrict);
        });

        model_builder.Entity<Ticket_message>(entity =>
        {
            entity.ToTable("ticket_messages");
            entity.Property(item => item.Id).HasColumnName("id");
            entity.Property(item => item.Ticket_id).HasColumnName("ticket_id");
            entity.Property(item => item.User_id).HasColumnName("user_id");
            entity.Property(item => item.Message_text).HasColumnName("message_text");
            entity.Property(item => item.Created_at).HasColumnName("created_at");

            entity.HasOne(item => item.Ticket)
                .WithMany(t => t.Messages)
                .HasForeignKey(item => item.Ticket_id)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(item => item.User)
                .WithMany()
                .HasForeignKey(item => item.User_id)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}

