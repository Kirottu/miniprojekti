PRAGMA foreign_keys=ON;

-- FIXME: How should authentication be handled?
DROP TABLE IF EXISTS users;
CREATE TABLE users(
    -- UUID
    id TEXT NOT NULL,
    name TEXT NOT NULL,
    PRIMARY KEY(id)  
);

DROP TABLE IF EXISTS references;
CREATE TABLE references(
    -- UUID
    id TEXT NOT NULL,
    -- The owner of the reference
    user TEXT NOT NULL,
    -- Mandatory for every entry
    entrytype TEXT NOT NULL,
    citekey TEXT NOT NULL,
    -- BibTeX fields
    address TEXT,
    annote TEXT,
    booktitle TEXT,
    chapter TEXT,
    edition TEXT,
    howpublished TEXT,
    institution TEXT,
    journal TEXT,
    month TEXT,
    note TEXT,
    number TEXT,
    organization TEXT,
    pages TEXT,
    publisher TEXT,
    school TEXT,
    series TEXT,
    title TEXT,
    type TEXT,
    volume TEXT,
    year TEXT,

    -- Non-standard fields
    doi TEXT,
    issn TEXT,
    isbn TEXT,
    url TEXT,
    FOREIGN KEY(user) REFERENCES users(id),
    PRIMARY KEY(id)
);

CREATE TABLE authors(
    reference TEXT NOT NULL,
    author TEXT NOT NULL,
    FOREIGN KEY(reference) REFERENCES references(id),
    PRIMARY KEY(reference, author)
);
