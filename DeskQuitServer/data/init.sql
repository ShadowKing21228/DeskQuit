-- Init schema for DeskQuit (PostgreSQL)

create table if not exists app_user (
                                        id uuid primary key,
                                        email text unique not null,
                                        password_hash text not null,
                                        password_salt text not null,
                                        created_at timestamptz not null default now()
    );

create table if not exists user_config (
                                           user_id uuid primary key references app_user(id) on delete cascade,
    afk_threshold_minutes int not null,
    timer_width double precision not null,
    timer_height double precision not null,
    run_on_startup boolean not null
    );

create table if not exists user_reminder (
                                             id text primary key, -- ReminderConfig.Id
                                             user_id uuid not null references app_user(id) on delete cascade,
    is_enabled boolean not null,
    interval_in_minutes int not null,
    notification_style int not null, -- enum stored as int
    is_custom boolean not null,
    custom_title text null,
    custom_description text null
    );

-- Daily aggregated stats. Missing row means "no stats for that day".
create table if not exists user_daily_stats (
                                                user_id uuid not null references app_user(id) on delete cascade,
    stat_date date not null,
    active_seconds bigint not null default 0,
    afk_seconds bigint not null default 0,
    notifications_total int not null default 0,
    notifications_custom int not null default 0,
    primary key (user_id, stat_date)
    );

-- Per-standard-reminder stats (custom reminders are aggregated in user_daily_stats.notifications_custom).
create table if not exists user_daily_reminder_stats (
                                                         user_id uuid not null references app_user(id) on delete cascade,
    stat_date date not null,
    reminder_id text not null,
    notifications_count int not null default 0,
    primary key (user_id, stat_date, reminder_id)
    );

-- Helpful indexes
create index if not exists idx_user_reminder_user_id
    on user_reminder (user_id);

create index if not exists idx_daily_stats_user_date
    on user_daily_stats (user_id, stat_date);

create index if not exists idx_daily_reminder_stats_user_date
    on user_daily_reminder_stats (user_id, stat_date);