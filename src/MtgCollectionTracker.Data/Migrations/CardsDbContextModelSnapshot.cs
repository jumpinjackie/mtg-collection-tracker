﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using MtgCollectionTracker.Data;

#nullable disable

namespace MtgCollectionTracker.Data.Migrations
{
    [DbContext(typeof(CardsDbContext))]
    partial class CardsDbContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder.HasAnnotation("ProductVersion", "8.0.8");

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

            modelBuilder.Entity("MtgCollectionTracker.Data.Notes", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<string>("Text")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("Title")
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.ToTable("Notes");
                });

            modelBuilder.Entity("MtgCollectionTracker.Data.ScryfallCardMetadata", b =>
                {
                    b.Property<string>("Id")
                        .HasMaxLength(36)
                        .HasColumnType("TEXT");

                    b.Property<byte[]>("BackImageLarge")
                        .HasColumnType("BLOB");

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

                    b.Property<byte[]>("ImageLarge")
                        .HasColumnType("BLOB");

                    b.Property<byte[]>("ImageSmall")
                        .HasColumnType("BLOB");

                    b.Property<string>("Language")
                        .HasMaxLength(3)
                        .HasColumnType("TEXT");

                    b.Property<int?>("ManaValue")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Rarity")
                        .IsRequired()
                        .HasMaxLength(11)
                        .HasColumnType("TEXT");

                    b.Property<string>("Type")
                        .HasMaxLength(64)
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.HasIndex("CardName", "Edition", "Language", "CollectorNumber");

                    b.ToTable("ScryfallCardMetadata");
                });

            modelBuilder.Entity("MtgCollectionTracker.Data.Tag", b =>
                {
                    b.Property<string>("Name")
                        .HasMaxLength(48)
                        .HasColumnType("TEXT");

                    b.HasKey("Name");

                    b.HasIndex("Name")
                        .IsUnique();

                    b.ToTable("Tag");
                });

            modelBuilder.Entity("MtgCollectionTracker.Data.Vendor", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasMaxLength(200)
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.ToTable("Vendors");
                });

            modelBuilder.Entity("MtgCollectionTracker.Data.VendorPrice", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<int>("AvailableStock")
                        .HasColumnType("INTEGER");

                    b.Property<int>("ItemId")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Notes")
                        .HasColumnType("TEXT");

                    b.Property<decimal>("Price")
                        .HasColumnType("TEXT");

                    b.Property<int>("VendorId")
                        .HasColumnType("INTEGER");

                    b.HasKey("Id");

                    b.HasIndex("ItemId");

                    b.HasIndex("VendorId");

                    b.ToTable("VendorPrice");
                });

            modelBuilder.Entity("MtgCollectionTracker.Data.WishlistItem", b =>
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

                    b.Property<int?>("Condition")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Edition")
                        .IsRequired()
                        .HasMaxLength(5)
                        .HasColumnType("TEXT");

                    b.Property<bool>("IsFoil")
                        .HasColumnType("INTEGER");

                    b.Property<bool>("IsLand")
                        .HasColumnType("INTEGER");

                    b.Property<string>("LanguageId")
                        .HasMaxLength(3)
                        .HasColumnType("TEXT");

                    b.Property<int>("Quantity")
                        .HasColumnType("INTEGER");

                    b.Property<string>("ScryfallId")
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.HasIndex("LanguageId");

                    b.HasIndex("ScryfallId");

                    b.ToTable("WishlistItems");
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

                    b.OwnsMany("MtgCollectionTracker.Data.CardSkuTag", "Tags", b1 =>
                        {
                            b1.Property<int>("Id")
                                .ValueGeneratedOnAdd()
                                .HasColumnType("INTEGER");

                            b1.Property<int>("CardSkuId")
                                .HasColumnType("INTEGER");

                            b1.Property<string>("Name")
                                .IsRequired()
                                .HasMaxLength(48)
                                .HasColumnType("TEXT");

                            b1.HasKey("Id");

                            b1.HasIndex("CardSkuId");

                            b1.HasIndex("Name");

                            b1.ToTable("CardSkuTag");

                            b1.WithOwner()
                                .HasForeignKey("CardSkuId");
                        });

                    b.Navigation("Container");

                    b.Navigation("Deck");

                    b.Navigation("Language");

                    b.Navigation("Scryfall");

                    b.Navigation("Tags");
                });

            modelBuilder.Entity("MtgCollectionTracker.Data.Deck", b =>
                {
                    b.HasOne("MtgCollectionTracker.Data.Container", "Container")
                        .WithMany("Decks")
                        .HasForeignKey("ContainerId");

                    b.Navigation("Container");
                });

            modelBuilder.Entity("MtgCollectionTracker.Data.VendorPrice", b =>
                {
                    b.HasOne("MtgCollectionTracker.Data.WishlistItem", "Item")
                        .WithMany("OfferedPrices")
                        .HasForeignKey("ItemId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("MtgCollectionTracker.Data.Vendor", "Vendor")
                        .WithMany("OfferedPrices")
                        .HasForeignKey("VendorId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Item");

                    b.Navigation("Vendor");
                });

            modelBuilder.Entity("MtgCollectionTracker.Data.WishlistItem", b =>
                {
                    b.HasOne("MtgCollectionTracker.Data.CardLanguage", "Language")
                        .WithMany()
                        .HasForeignKey("LanguageId");

                    b.HasOne("MtgCollectionTracker.Data.ScryfallCardMetadata", "Scryfall")
                        .WithMany()
                        .HasForeignKey("ScryfallId");

                    b.OwnsMany("MtgCollectionTracker.Data.WishlistItemTag", "Tags", b1 =>
                        {
                            b1.Property<int>("Id")
                                .ValueGeneratedOnAdd()
                                .HasColumnType("INTEGER");

                            b1.Property<string>("Name")
                                .IsRequired()
                                .HasMaxLength(48)
                                .HasColumnType("TEXT");

                            b1.Property<int>("WishlistItemId")
                                .HasColumnType("INTEGER");

                            b1.HasKey("Id");

                            b1.HasIndex("Name");

                            b1.HasIndex("WishlistItemId");

                            b1.ToTable("WishlistItemTag");

                            b1.WithOwner()
                                .HasForeignKey("WishlistItemId");
                        });

                    b.Navigation("Language");

                    b.Navigation("Scryfall");

                    b.Navigation("Tags");
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

            modelBuilder.Entity("MtgCollectionTracker.Data.Vendor", b =>
                {
                    b.Navigation("OfferedPrices");
                });

            modelBuilder.Entity("MtgCollectionTracker.Data.WishlistItem", b =>
                {
                    b.Navigation("OfferedPrices");
                });
#pragma warning restore 612, 618
        }
    }
}
