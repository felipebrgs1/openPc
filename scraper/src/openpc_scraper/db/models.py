"""Modelos SQLAlchemy — espelho EXATO do schema EF Core (OpenPc.Infrastructure).

O EF Core cita identificadores: colunas PascalCase entre aspas ("Id",
"StoreSku"...), tabelas lowercase (products, listings...). Os atributos
Python ficam em snake_case; o nome físico vai explícito em mapped_column.
"""

from __future__ import annotations

import uuid
from datetime import datetime

from sqlalchemy import (
    BigInteger,
    Boolean,
    DateTime,
    ForeignKey,
    Integer,
    Numeric,
    String,
    Text,
    UniqueConstraint,
    func,
)
from sqlalchemy.orm import DeclarativeBase, Mapped, mapped_column, relationship


class Base(DeclarativeBase):
    pass


def _uuid() -> uuid.UUID:
    return uuid.uuid4()


def _now() -> datetime:
    return datetime.utcnow()


class Store(Base):
    __tablename__ = "stores"

    id: Mapped[uuid.UUID] = mapped_column("Id", primary_key=True, default=_uuid)
    slug: Mapped[str] = mapped_column("Slug", String(64))
    name: Mapped[str] = mapped_column("Name", String(128))
    base_url: Mapped[str] = mapped_column("BaseUrl", String(256))
    is_active: Mapped[bool] = mapped_column("IsActive", Boolean, default=True)
    created_at: Mapped[datetime] = mapped_column("CreatedAt", DateTime(timezone=True), default=_now)


class Category(Base):
    __tablename__ = "categories"

    id: Mapped[uuid.UUID] = mapped_column("Id", primary_key=True, default=_uuid)
    slug: Mapped[str] = mapped_column("Slug", String(64))
    name: Mapped[str] = mapped_column("Name", String(128))
    display_order: Mapped[int] = mapped_column("DisplayOrder", Integer, default=0)
    created_at: Mapped[datetime] = mapped_column("CreatedAt", DateTime(timezone=True), default=_now)


class Product(Base):
    __tablename__ = "products"

    id: Mapped[uuid.UUID] = mapped_column("Id", primary_key=True, default=_uuid)
    category_id: Mapped[uuid.UUID] = mapped_column("CategoryId", ForeignKey("categories.Id"))
    brand: Mapped[str] = mapped_column("Brand", String(64))
    model: Mapped[str] = mapped_column("Model", String(128))
    name: Mapped[str] = mapped_column("Name", String(512))
    part_number: Mapped[str | None] = mapped_column("PartNumber", String(64), index=True)
    ean: Mapped[str | None] = mapped_column("Ean", String(32))
    image_url: Mapped[str | None] = mapped_column("ImageUrl", String(512))
    spec_source: Mapped[str] = mapped_column("SpecSource", String(16), default="scraper")
    created_at: Mapped[datetime] = mapped_column("CreatedAt", DateTime(timezone=True), default=_now)
    updated_at: Mapped[datetime] = mapped_column("UpdatedAt", DateTime(timezone=True), default=_now, onupdate=_now)

    category: Mapped[Category] = relationship()
    attributes: Mapped[list[ProductAttribute]] = relationship(back_populates="product", cascade="all, delete-orphan")
    listings: Mapped[list[Listing]] = relationship(back_populates="product", cascade="all, delete-orphan")


class ProductAttribute(Base):
    __tablename__ = "product_attributes"
    __table_args__ = (UniqueConstraint("ProductId", "Key"),)

    id: Mapped[uuid.UUID] = mapped_column("Id", primary_key=True, default=_uuid)
    product_id: Mapped[uuid.UUID] = mapped_column("ProductId", ForeignKey("products.Id", ondelete="CASCADE"))
    key: Mapped[str] = mapped_column("Key", String(48))
    value_text: Mapped[str | None] = mapped_column("ValueText", String(256))
    value_num: Mapped[float | None] = mapped_column("ValueNum", Numeric(12, 2))
    value_bool: Mapped[bool | None] = mapped_column("ValueBool", Boolean)
    source: Mapped[str] = mapped_column("Source", String(16), default="title")  # reference|title|page|manual

    product: Mapped[Product] = relationship(back_populates="attributes")


class Listing(Base):
    __tablename__ = "listings"
    __table_args__ = (UniqueConstraint("StoreId", "StoreSku"),)

    id: Mapped[uuid.UUID] = mapped_column("Id", primary_key=True, default=_uuid)
    product_id: Mapped[uuid.UUID] = mapped_column("ProductId", ForeignKey("products.Id", ondelete="CASCADE"))
    store_id: Mapped[uuid.UUID] = mapped_column("StoreId", ForeignKey("stores.Id"))
    store_sku: Mapped[str] = mapped_column("StoreSku", String(256))
    url: Mapped[str] = mapped_column("Url", String(512))
    title: Mapped[str] = mapped_column("Title", String(512))
    price_cash: Mapped[float | None] = mapped_column("PriceCash", Numeric(12, 2))
    price_card: Mapped[float | None] = mapped_column("PriceCard", Numeric(12, 2))
    installments: Mapped[int | None] = mapped_column("Installments", Integer)
    installment_text: Mapped[str | None] = mapped_column("InstallmentText", String(64))
    in_stock: Mapped[bool] = mapped_column("InStock", Boolean, default=False)
    thumbnail: Mapped[str | None] = mapped_column("Thumbnail", String(512))
    last_seen_at: Mapped[datetime] = mapped_column("LastSeenAt", DateTime(timezone=True), default=_now)
    specs_collected_at: Mapped[datetime | None] = mapped_column("SpecsCollectedAt", DateTime(timezone=True))
    created_at: Mapped[datetime] = mapped_column("CreatedAt", DateTime(timezone=True), default=_now)

    product: Mapped[Product] = relationship(back_populates="listings")
    price_history: Mapped[list[PriceHistory]] = relationship(back_populates="listing", cascade="all, delete-orphan")


class PriceHistory(Base):
    __tablename__ = "price_history"

    id: Mapped[uuid.UUID] = mapped_column("Id", primary_key=True, default=_uuid)
    listing_id: Mapped[uuid.UUID] = mapped_column("ListingId", ForeignKey("listings.Id", ondelete="CASCADE"))
    price_cash: Mapped[float] = mapped_column("PriceCash", Numeric(12, 2))
    price_card: Mapped[float | None] = mapped_column("PriceCard", Numeric(12, 2))
    in_stock: Mapped[bool] = mapped_column("InStock", Boolean, default=False)
    collected_at: Mapped[datetime] = mapped_column("CollectedAt", DateTime(timezone=True), default=_now)

    listing: Mapped[Listing] = relationship(back_populates="price_history")


class PriceDaily(Base):
    __tablename__ = "price_daily"
    __table_args__ = (UniqueConstraint("ProductId", "Date"),)

    id: Mapped[uuid.UUID] = mapped_column("Id", primary_key=True, default=_uuid)
    product_id: Mapped[uuid.UUID] = mapped_column("ProductId", ForeignKey("products.Id", ondelete="CASCADE"))
    date: Mapped[datetime] = mapped_column("Date", DateTime(timezone=True))
    min_price: Mapped[float] = mapped_column("MinPrice", Numeric(12, 2))
    listing_id: Mapped[uuid.UUID | None] = mapped_column("ListingId", ForeignKey("listings.Id"))
    updated_at: Mapped[datetime] = mapped_column("UpdatedAt", DateTime(timezone=True), default=_now, onupdate=_now)


class PriceAlert(Base):
    __tablename__ = "price_alerts"

    id: Mapped[uuid.UUID] = mapped_column("Id", primary_key=True, default=_uuid)
    product_id: Mapped[uuid.UUID] = mapped_column("ProductId", ForeignKey("products.Id", ondelete="CASCADE"))
    email: Mapped[str] = mapped_column("Email", String(320))
    target_price: Mapped[float] = mapped_column("TargetPrice", Numeric(12, 2))
    token: Mapped[str] = mapped_column("Token", String(64))
    confirmed: Mapped[bool] = mapped_column("Confirmed", Boolean, default=False)
    confirmed_at: Mapped[datetime | None] = mapped_column("ConfirmedAt", DateTime(timezone=True))
    last_triggered_at: Mapped[datetime | None] = mapped_column("LastTriggeredAt", DateTime(timezone=True))
    trigger_count: Mapped[int] = mapped_column("TriggerCount", Integer, default=0)
    created_at: Mapped[datetime] = mapped_column("CreatedAt", DateTime(timezone=True), default=_now)


class PriceAlertEvent(Base):
    __tablename__ = "price_alert_events"

    id: Mapped[uuid.UUID] = mapped_column("Id", primary_key=True, default=_uuid)
    alert_id: Mapped[uuid.UUID] = mapped_column("AlertId", ForeignKey("price_alerts.Id", ondelete="CASCADE"))
    listing_id: Mapped[uuid.UUID] = mapped_column("ListingId", ForeignKey("listings.Id"))
    price_at_trigger: Mapped[float] = mapped_column("PriceAtTrigger", Numeric(12, 2))
    email_sent: Mapped[bool] = mapped_column("EmailSent", Boolean, default=False)
    triggered_at: Mapped[datetime] = mapped_column("TriggeredAt", DateTime(timezone=True), default=_now)


class ScrapeJob(Base):
    __tablename__ = "scrape_jobs"

    id: Mapped[uuid.UUID] = mapped_column("Id", primary_key=True, default=_uuid)
    store_id: Mapped[uuid.UUID] = mapped_column("StoreId", ForeignKey("stores.Id"))
    category_id: Mapped[uuid.UUID] = mapped_column("CategoryId", ForeignKey("categories.Id"))
    schedule_cron: Mapped[str] = mapped_column("ScheduleCron", String(32))
    enabled: Mapped[bool] = mapped_column("Enabled", Boolean, default=True)

    store: Mapped[Store] = relationship()
    category: Mapped[Category] = relationship()


class ScrapeRun(Base):
    __tablename__ = "scrape_runs"

    id: Mapped[uuid.UUID] = mapped_column("Id", primary_key=True, default=_uuid)
    job_id: Mapped[uuid.UUID] = mapped_column("JobId", ForeignKey("scrape_jobs.Id", ondelete="CASCADE"))
    status: Mapped[str] = mapped_column("Status", String(16))
    items_found: Mapped[int] = mapped_column("ItemsFound", Integer, default=0)
    items_new: Mapped[int] = mapped_column("ItemsNew", Integer, default=0)
    error: Mapped[str | None] = mapped_column("Error", Text)
    duration_ms: Mapped[int] = mapped_column("DurationMs", BigInteger, default=0)
    started_at: Mapped[datetime] = mapped_column("StartedAt", DateTime(timezone=True), default=_now)
    finished_at: Mapped[datetime | None] = mapped_column("FinishedAt", DateTime(timezone=True))

    job: Mapped[ScrapeJob] = relationship()


class ProductMatchCandidate(Base):
    __tablename__ = "product_match_candidates"

    id: Mapped[uuid.UUID] = mapped_column("Id", primary_key=True, default=_uuid)
    product_id: Mapped[uuid.UUID] = mapped_column("ProductId", ForeignKey("products.Id", ondelete="CASCADE"))
    store_id: Mapped[uuid.UUID] = mapped_column("StoreId", ForeignKey("stores.Id"))
    store_sku: Mapped[str] = mapped_column("StoreSku", String(256))
    title: Mapped[str] = mapped_column("Title", String(512))
    reason: Mapped[str] = mapped_column("Reason", String(32))
    similarity: Mapped[float | None] = mapped_column("Similarity", Numeric(4, 3))
    created_at: Mapped[datetime] = mapped_column("CreatedAt", DateTime(timezone=True), default=_now)
