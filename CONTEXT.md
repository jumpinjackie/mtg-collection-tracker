# Context — MtG Collection Tracker

## Glossary

- **Wishlist Item** — A desired quantity of a specific card printing, tracked independently from the physical collection. May have vendor price offers and tags attached.
- **In-Transit Quantity** (`InTransitQuantity`) — The portion of a wishlist item's desired quantity that has been ordered/shipped but not yet received into the physical collection. Persisted as `int` on `WishlistItem` (default `0`). Always ≤ the wishlist item's total `Quantity` (enforced as a hard error on update). When `Quantity` is reduced below `InTransitQuantity`, the in-transit value is auto-clamped to the new quantity. Only displayed when > 0, shown parenthesized beside the quantity label (e.g. `Qty: 4 (2)`). Settable via the edit dialog only (not on initial add), using an `ApplyInTransit` checkbox like other fields. Applies to both real and proxy wishlist items. Not affected by move-to-collection operations. The buying list computes effective-needed as `Quantity - InTransitQuantity`. NSwag client regeneration is a separate step after implementation.
