﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using MtgCollectionTracker.Data;

#nullable disable

namespace MtgCollectionTracker.Data.Migrations
{
    [DbContext(typeof(CardsDbContext))]
    [Migration("20240401142259_CardLanguage")]
    partial class CardLanguage
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder.HasAnnotation("ProductVersion", "8.0.3");

            modelBuilder.Entity("MtgCollectionTracker.Data.CardLanguage", b =>
                {
                    b.Property<string>("Code")
                        .HasColumnType("TEXT");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasMaxLength(20)
                        .HasColumnType("TEXT");

                    b.Property<string>("PrintedCode")
                        .HasMaxLength(3)
                        .HasColumnType("TEXT");

                    b.HasKey("Code");

                    b.ToTable("CardLanguage");

                    b.HasData(
                        new
                        {
                            Code = "en",
                            Name = "English",
                            PrintedCode = "en"
                        },
                        new
                        {
                            Code = "es",
                            Name = "Spanish",
                            PrintedCode = "sp"
                        },
                        new
                        {
                            Code = "fr",
                            Name = "French",
                            PrintedCode = "fr"
                        },
                        new
                        {
                            Code = "de",
                            Name = "German",
                            PrintedCode = "de"
                        },
                        new
                        {
                            Code = "it",
                            Name = "Italian",
                            PrintedCode = "it"
                        },
                        new
                        {
                            Code = "pt",
                            Name = "Portuguese",
                            PrintedCode = "pt"
                        },
                        new
                        {
                            Code = "ja",
                            Name = "Japanese",
                            PrintedCode = "jp"
                        },
                        new
                        {
                            Code = "ko",
                            Name = "Korean",
                            PrintedCode = "kr"
                        },
                        new
                        {
                            Code = "ru",
                            Name = "Russian",
                            PrintedCode = "ru"
                        },
                        new
                        {
                            Code = "zhs",
                            Name = "Simplified Chinese",
                            PrintedCode = "cs"
                        },
                        new
                        {
                            Code = "zht",
                            Name = "Traditional Chinese",
                            PrintedCode = "ct"
                        },
                        new
                        {
                            Code = "he",
                            Name = "Hebrew"
                        },
                        new
                        {
                            Code = "la",
                            Name = "Latin"
                        },
                        new
                        {
                            Code = "grc",
                            Name = "Ancient Greek"
                        },
                        new
                        {
                            Code = "ar",
                            Name = "Arabic"
                        },
                        new
                        {
                            Code = "sa",
                            Name = "Sanskrit"
                        },
                        new
                        {
                            Code = "ph",
                            Name = "Phyrexian",
                            PrintedCode = "ph"
                        });
                });

            modelBuilder.Entity("MtgCollectionTracker.Data.CardSku", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<string>("CardName")
                        .IsRequired()
                        .HasMaxLength(256)
                        .HasColumnType("TEXT");

                    b.Property<string>("CollectorNumber")
                        .HasMaxLength(5)
                        .HasColumnType("TEXT");

                    b.Property<string>("Comments")
                        .HasMaxLength(256)
                        .HasColumnType("TEXT");

                    b.Property<int?>("Condition")
                        .HasColumnType("INTEGER");

                    b.Property<int?>("ContainerId")
                        .HasColumnType("INTEGER");

                    b.Property<int?>("DeckId")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Edition")
                        .IsRequired()
                        .HasMaxLength(5)
                        .HasColumnType("TEXT");

                    b.Property<bool>("IsFoil")
                        .HasColumnType("INTEGER");

                    b.Property<bool>("IsLand")
                        .HasColumnType("INTEGER");

                    b.Property<bool>("IsSideboard")
                        .HasColumnType("INTEGER");

                    b.Property<string>("LanguageId")
                        .HasMaxLength(3)
                        .HasColumnType("TEXT");

                    b.Property<int>("Quantity")
                        .HasColumnType("INTEGER");

                    b.Property<string>("ScryfallId")
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.HasIndex("CardName");

                    b.HasIndex("ContainerId");

                    b.HasIndex("DeckId");

                    b.HasIndex("LanguageId");

                    b.HasIndex("ScryfallId");

                    b.ToTable("Cards");
                });

            modelBuilder.Entity("MtgCollectionTracker.Data.Container", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<string>("Description")
                        .HasMaxLength(256)
                        .HasColumnType("TEXT");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasMaxLength(64)
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.HasIndex("Name")
                        .IsUnique();

                    b.ToTable("Containers");
                });

            modelBuilder.Entity("MtgCollectionTracker.Data.Deck", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<int?>("ContainerId")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Format")
                        .HasMaxLength(32)
                        .HasColumnType("TEXT");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasMaxLength(64)
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.HasIndex("ContainerId");

                    b.HasIndex("Name")
                        .IsUnique();

                    b.ToTable("Decks");
                });

            modelBuilder.Entity("MtgCollectionTracker.Data.ScryfallCardMetadata", b =>
                {
                    b.Property<string>("Id")
                        .HasMaxLength(36)
                        .HasColumnType("TEXT");

                    b.Property<byte[]>("BackImageSmall")
                        .HasColumnType("BLOB");

                    b.Property<string>("CardName")
                        .IsRequired()
                        .HasMaxLength(256)
                        .HasColumnType("TEXT");

                    b.Property<string>("CardType")
                        .IsRequired()
                        .HasMaxLength(128)
                        .HasColumnType("TEXT");

                    b.Property<string>("CollectorNumber")
                        .HasMaxLength(5)
                        .HasColumnType("TEXT");

                    b.Property<string>("Edition")
                        .IsRequired()
                        .HasMaxLength(5)
                        .HasColumnType("TEXT");

                    b.Property<byte[]>("ImageSmall")
                        .HasColumnType("BLOB");

                    b.Property<string>("Language")
                        .HasMaxLength(3)
                        .HasColumnType("TEXT");

                    b.Property<string>("Rarity")
                        .IsRequired()
                        .HasMaxLength(11)
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.HasIndex("CardName", "Edition", "Language", "CollectorNumber");

                    b.ToTable("ScryfallCardMetadata");
                });

            modelBuilder.Entity("MtgCollectionTracker.Data.CardSku", b =>
                {
                    b.HasOne("MtgCollectionTracker.Data.Container", "Container")
                        .WithMany("Cards")
                        .HasForeignKey("ContainerId");

                    b.HasOne("MtgCollectionTracker.Data.Deck", "Deck")
                        .WithMany("Cards")
                        .HasForeignKey("DeckId");

                    b.HasOne("MtgCollectionTracker.Data.CardLanguage", "Language")
                        .WithMany()
                        .HasForeignKey("LanguageId");

                    b.HasOne("MtgCollectionTracker.Data.ScryfallCardMetadata", "Scryfall")
                        .WithMany()
                        .HasForeignKey("ScryfallId");

                    b.Navigation("Container");

                    b.Navigation("Deck");

                    b.Navigation("Language");

                    b.Navigation("Scryfall");
                });

            modelBuilder.Entity("MtgCollectionTracker.Data.Deck", b =>
                {
                    b.HasOne("MtgCollectionTracker.Data.Container", "Container")
                        .WithMany("Decks")
                        .HasForeignKey("ContainerId");

                    b.Navigation("Container");
                });

            modelBuilder.Entity("MtgCollectionTracker.Data.Container", b =>
                {
                    b.Navigation("Cards");

                    b.Navigation("Decks");
                });

            modelBuilder.Entity("MtgCollectionTracker.Data.Deck", b =>
                {
                    b.Navigation("Cards");
                });
#pragma warning restore 612, 618
        }
    }
}
