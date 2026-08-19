CREATE TABLE users (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    username TEXT NOT NULL
        CONSTRAINT ck_users_username_length CHECK (length(username) BETWEEN 1 AND 50),
    provider INTEGER NOT NULL
        CONSTRAINT ck_users_provider CHECK (provider IN (1, 2, 999)),
    provider_user_id TEXT NOT NULL
        CONSTRAINT ck_users_provider_user_id_length CHECK (length(provider_user_id) BETWEEN 1 AND 255),
    created_at_utc_ms INTEGER NOT NULL
);

CREATE UNIQUE INDEX ux_users_username
    ON users (username);

CREATE UNIQUE INDEX ux_users_provider_provider_user_id
    ON users (provider, provider_user_id);

CREATE TABLE refresh_tokens (
    id TEXT PRIMARY KEY
        CONSTRAINT ck_refresh_tokens_id CHECK (length(id) = 36 AND id = lower(id)),
    user_id INTEGER NOT NULL,
    family_id TEXT NOT NULL
        CONSTRAINT ck_refresh_tokens_family_id CHECK (length(family_id) = 36 AND family_id = lower(family_id)),
    token_hash BLOB NOT NULL
        CONSTRAINT ck_refresh_tokens_token_hash CHECK (typeof(token_hash) = 'blob' AND length(token_hash) = 32),
    created_at_utc_ms INTEGER NOT NULL,
    expires_at_utc_ms INTEGER NOT NULL,
    used_at_utc_ms INTEGER NULL,
    revoked_at_utc_ms INTEGER NULL,
    revoke_reason TEXT NULL
        CONSTRAINT ck_refresh_tokens_revoke_reason CHECK (revoke_reason IS NULL OR length(revoke_reason) <= 64),
    replaced_by_token_id TEXT NULL,
    CONSTRAINT fk_refresh_tokens_users
        FOREIGN KEY (user_id) REFERENCES users (id) ON DELETE CASCADE,
    CONSTRAINT fk_refresh_tokens_replaced_by_token
        FOREIGN KEY (replaced_by_token_id) REFERENCES refresh_tokens (id)
);

CREATE UNIQUE INDEX ux_refresh_tokens_token_hash
    ON refresh_tokens (token_hash);

CREATE INDEX ix_refresh_tokens_family_id
    ON refresh_tokens (family_id);

CREATE INDEX ix_refresh_tokens_user_id_revoked_at
    ON refresh_tokens (user_id, revoked_at_utc_ms);

CREATE INDEX ix_refresh_tokens_expires_at
    ON refresh_tokens (expires_at_utc_ms);

CREATE TABLE characters (
    user_id INTEGER PRIMARY KEY,
    level INTEGER NOT NULL
        CONSTRAINT ck_characters_level CHECK (level >= 1),
    exp INTEGER NOT NULL
        CONSTRAINT ck_characters_exp CHECK (exp >= 0),
    CONSTRAINT fk_characters_users
        FOREIGN KEY (user_id) REFERENCES users (id) ON DELETE CASCADE
);

CREATE TABLE currencies (
    user_id INTEGER NOT NULL,
    type INTEGER NOT NULL
        CONSTRAINT ck_currencies_type CHECK (type IN (1, 2, 3, 4)),
    amount INTEGER NOT NULL
        CONSTRAINT ck_currencies_amount CHECK (amount >= 0),
    PRIMARY KEY (user_id, type),
    CONSTRAINT fk_currencies_users
        FOREIGN KEY (user_id) REFERENCES users (id) ON DELETE CASCADE
);

CREATE TABLE stage_runs (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    user_id INTEGER NOT NULL,
    stage_id INTEGER NOT NULL,
    status INTEGER NOT NULL
        CONSTRAINT ck_stage_runs_status CHECK (status IN (0, 1, 2)),
    started_at_utc_ms INTEGER NOT NULL,
    completed_at_utc_ms INTEGER NULL,
    exp_gained INTEGER NOT NULL DEFAULT 0
        CONSTRAINT ck_stage_runs_exp_gained CHECK (exp_gained >= 0),
    currencies_gained_json TEXT NULL,
    CONSTRAINT fk_stage_runs_users
        FOREIGN KEY (user_id) REFERENCES users (id) ON DELETE CASCADE
);

CREATE INDEX ix_stage_runs_user_id_status
    ON stage_runs (user_id, status);

CREATE UNIQUE INDEX ux_stage_runs_user_in_progress
    ON stage_runs (user_id)
    WHERE status = 0;

CREATE TABLE user_rooms (
    user_id INTEGER PRIMARY KEY,
    map_id INTEGER NOT NULL,
    traps_json TEXT NOT NULL
        CONSTRAINT ck_user_rooms_traps_json CHECK (
            json_valid(traps_json) AND json_type(traps_json) = 'array'
        ),
    updated_at_utc_ms INTEGER NOT NULL,
    CONSTRAINT fk_user_rooms_users
        FOREIGN KEY (user_id) REFERENCES users (id) ON DELETE CASCADE
);
