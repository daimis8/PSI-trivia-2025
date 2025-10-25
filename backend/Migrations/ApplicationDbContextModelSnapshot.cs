using backend.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace backend.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    partial class ApplicationDbContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "9.0.0")
                .HasAnnotation("Relational:MaxIdentifierLength", 63);

            NpgsqlModelBuilderExtensions.UseIdentityByDefaultColumns(modelBuilder);

            modelBuilder.Entity("backend.Models.Quiz", b =>
            {
                b.Property<int>("ID")
                    .ValueGeneratedOnAdd()
                    .HasColumnType("integer");

                NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("ID"));

                b.Property<int>("CreatorID")
                    .HasColumnType("integer");

                b.Property<string>("Description")
                    .IsRequired()
                    .HasMaxLength(1000)
                    .HasColumnType("character varying(1000)");

                b.Property<string>("Questions")
                    .IsRequired()
                    .HasColumnType("jsonb");

                b.Property<string>("Title")
                    .IsRequired()
                    .HasMaxLength(255)
                    .HasColumnType("character varying(255)");

                b.HasKey("ID");

                b.HasIndex("CreatorID");

                b.ToTable("Quizzes");
            });

            modelBuilder.Entity("backend.Models.User", b =>
            {
                b.Property<int>("Id")
                    .ValueGeneratedOnAdd()
                    .HasColumnType("integer");

                NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                b.Property<string>("Email")
                    .IsRequired()
                    .HasMaxLength(255)
                    .HasColumnType("character varying(255)");

                b.Property<string>("Password")
                    .IsRequired()
                    .HasColumnType("text");

                b.Property<string>("Username")
                    .IsRequired()
                    .HasMaxLength(255)
                    .HasColumnType("character varying(255)");

                b.HasKey("Id");

                b.HasIndex("Email")
                    .IsUnique();

                b.HasIndex("Username")
                    .IsUnique();

                b.ToTable("Users");
            });
#pragma warning restore 612, 618
        }
    }
}
