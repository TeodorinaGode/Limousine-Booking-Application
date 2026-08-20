import { useMemo, useState } from "react";
import { useNavigate } from "react-router-dom";
import { useTranslation } from "react-i18next";
import { MapContainer, TileLayer, Marker, Popup, Polyline, Circle, useMap } from "react-leaflet";
import L from "leaflet";
import "leaflet/dist/leaflet.css";
import type { PublicLocationDto } from "../types/location";
import type { PublicRouteDto } from "../types/booking";

const TILE_URL = import.meta.env.VITE_MAP_TILE_URL ?? "https://{s}.basemaps.cartocdn.com/dark_all/{z}/{x}/{y}{r}.png";
const TILE_ATTRIBUTION =
  '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors &copy; <a href="https://carto.com/attributions">CARTO</a>';

/** Approximate geographic centre of Switzerland — used only to softly emphasize the primary market (section 11/12), never a precise business address. */
const SWITZERLAND_CENTER: [number, number] = [46.8182, 8.2275];
const SWITZERLAND_EMPHASIS_RADIUS_METERS = 140_000;

const MARKER_ICONS: Record<PublicLocationDto["type"], L.DivIcon> = {
  City: L.divIcon({ className: "map-marker map-marker--city", html: "●", iconSize: [14, 14], iconAnchor: [7, 7] }),
  Airport: L.divIcon({ className: "map-marker map-marker--airport", html: "&#9992;", iconSize: [18, 18], iconAnchor: [9, 9] }),
  Destination: L.divIcon({ className: "map-marker map-marker--destination", html: "&#9670;", iconSize: [12, 12], iconAnchor: [6, 6] }),
};

interface MatchedRoute {
  route: PublicRouteDto;
  from: PublicLocationDto;
  to: PublicLocationDto;
}

interface ServiceAreaMapProps {
  locations: PublicLocationDto[];
  routes: PublicRouteDto[];
  defaultLatitude: number;
  defaultLongitude: number;
  defaultZoom: number;
  selectedRouteId?: string | null;
  onSelectRoute?: (routeId: string | null) => void;
  height?: number;
}

function normalize(value: string): string {
  return value.trim().toLowerCase();
}

/** Recenters/zooms the map imperatively when the controlled `selectedRouteId` prop changes — react-leaflet has no declarative "fit these bounds" prop, so this is the documented escape hatch (`useMap()`). */
function FlyToRoute({ matched }: { matched: MatchedRoute | undefined }) {
  const map = useMap();

  if (matched) {
    const bounds = L.latLngBounds(
      [matched.from.latitude, matched.from.longitude],
      [matched.to.latitude, matched.to.longitude]
    );
    map.flyToBounds(bounds, { padding: [60, 60], maxZoom: 9 });
  }

  return null;
}

/**
 * Provider-agnostic service-area map (Prompt 19, section 13) — all
 * Leaflet-specific code lives here; callers only ever see `locations`,
 * `routes`, and plain callbacks, so switching to Mapbox/Google later means
 * rewriting this one file, not every page that embeds a map. Dynamically
 * imported by every caller via `React.lazy` (section 18), so the map
 * library is never in the main bundle.
 *
 * Deliberately draws a line only for a route whose departure AND
 * destination both match a real `Location` pin by name — a `Location` on
 * the map (e.g. Milan) does not imply a bookable route exists to it
 * (section 25). `routes` always comes from the live `GET /api/public/routes`
 * response, never hard-coded.
 */
function ServiceAreaMap({ locations, routes, defaultLatitude, defaultLongitude, defaultZoom, selectedRouteId, onSelectRoute, height = 420 }: ServiceAreaMapProps) {
  const { t } = useTranslation("site");
  const navigate = useNavigate();
  const [activeRoute, setActiveRoute] = useState<MatchedRoute | null>(null);

  const locationsByName = useMemo(() => {
    const map = new Map<string, PublicLocationDto>();
    for (const location of locations) map.set(normalize(location.name), location);
    return map;
  }, [locations]);

  const matchedRoutes = useMemo<MatchedRoute[]>(() => {
    const result: MatchedRoute[] = [];
    for (const route of routes) {
      const from = locationsByName.get(normalize(route.departureLocation));
      const to = locationsByName.get(normalize(route.destination));
      if (from && to) result.push({ route, from, to });
    }
    return result;
  }, [routes, locationsByName]);

  const routesByDeparture = useMemo(() => {
    const map = new Map<string, MatchedRoute[]>();
    for (const matched of matchedRoutes) {
      const key = matched.from.id;
      const existing = map.get(key) ?? [];
      existing.push(matched);
      map.set(key, existing);
    }
    return map;
  }, [matchedRoutes]);

  const selectedMatch = matchedRoutes.find((m) => m.route.id === selectedRouteId);
  const displayedRoute = activeRoute ?? selectedMatch ?? null;

  const selectRoute = (matched: MatchedRoute) => {
    setActiveRoute(matched);
    onSelectRoute?.(matched.route.id);
  };

  const handleBookRoute = (routeId: string) => {
    navigate("/booking", { state: { routeId } });
  };

  return (
    <div className="service-area-map">
      <div className="service-area-map__canvas" style={{ height }} role="region" aria-label={t("serviceAreaMap.regionLabel")}>
        <MapContainer center={[defaultLatitude, defaultLongitude]} zoom={defaultZoom} scrollWheelZoom={false} style={{ height: "100%", width: "100%" }}>
          <TileLayer url={TILE_URL} attribution={TILE_ATTRIBUTION} />
          <Circle
            center={SWITZERLAND_CENTER}
            radius={SWITZERLAND_EMPHASIS_RADIUS_METERS}
            pathOptions={{ color: "var(--color-accent)", fillColor: "var(--color-accent)", fillOpacity: 0.06, weight: 1, opacity: 0.25 }}
          />

          {matchedRoutes.map((matched) => (
            <Polyline
              key={matched.route.id}
              positions={[
                [matched.from.latitude, matched.from.longitude],
                [matched.to.latitude, matched.to.longitude],
              ]}
              pathOptions={{
                color: displayedRoute?.route.id === matched.route.id ? "var(--color-text-primary)" : "var(--color-text-muted)",
                weight: displayedRoute?.route.id === matched.route.id ? 3 : 1.5,
                opacity: displayedRoute?.route.id === matched.route.id ? 0.9 : 0.45,
              }}
              eventHandlers={{ click: () => selectRoute(matched) }}
            />
          ))}

          {locations.map((location) => {
            const departures = routesByDeparture.get(location.id) ?? [];
            return (
              <Marker key={location.id} position={[location.latitude, location.longitude]} icon={MARKER_ICONS[location.type]}>
                <Popup>
                  <div className="map-popup">
                    <p className="map-popup__name">{location.name}</p>
                    <p className="map-popup__meta">{t(`operatingArea.countries.${location.countryCode}`, { defaultValue: location.countryCode })}</p>
                    {location.description && <p className="map-popup__desc">{location.description}</p>}
                    {departures.length > 0 ? (
                      <div className="map-popup__routes">
                        <p className="map-popup__routes-label">{t("serviceAreaMap.availableDestinations")}</p>
                        {departures.map((matched) => (
                          <button
                            key={matched.route.id}
                            type="button"
                            className="map-popup__route-button"
                            onClick={() => handleBookRoute(matched.route.id)}
                          >
                            {matched.to.name} &middot; {matched.route.currency} {matched.route.price.toFixed(0)}
                          </button>
                        ))}
                      </div>
                    ) : (
                      <a className="map-popup__cta" href="/booking">
                        {t("nav.bookARide")}
                      </a>
                    )}
                  </div>
                </Popup>
              </Marker>
            );
          })}

          <FlyToRoute matched={selectedMatch} />
        </MapContainer>
      </div>

      {displayedRoute && (
        <div className="map-route-card">
          <p className="map-route-card__route">
            {displayedRoute.from.name} <span className="trip-card__arrow">&rarr;</span> {displayedRoute.to.name}
          </p>
          <p className="map-route-card__meta">
            {t("routesTeaser.duration")}: {Math.floor(displayedRoute.route.estimatedDurationMinutes / 60)}h {displayedRoute.route.estimatedDurationMinutes % 60}m
            {" · "}
            {t("routesTeaser.from")} {displayedRoute.route.price.toFixed(2)} {displayedRoute.route.currency}
          </p>
          <button type="button" onClick={() => handleBookRoute(displayedRoute.route.id)}>
            {t("serviceAreaMap.bookThisRoute")}
          </button>
        </div>
      )}

      <div className="map-location-list">
        <p className="map-location-list__title">{t("serviceAreaMap.mainDestinations")}</p>
        <ul>
          {locations.map((location) => (
            <li key={location.id}>{location.name}</li>
          ))}
        </ul>
      </div>
    </div>
  );
}

export default ServiceAreaMap;
