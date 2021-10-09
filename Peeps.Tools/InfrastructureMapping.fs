namespace Peeps.Tools

open System.Text.Json.Serialization
open Freql.Core.Common.Types
open Freql.Sqlite

module InfrastructureMapping =

    type MetadataItem =
        { [<JsonPropertyName("key")>]
          Key: string
          [<JsonPropertyName("value")>]
          Value: string }

    type DocumentItem =
        { [<JsonPropertyName("id")>]
          Id: string
          [<JsonPropertyName("name")>]
          Name: string
          [<JsonPropertyName("description")>]
          Description: string }

    type Connection =
        { [<JsonPropertyName("id")>]
          Id: string
          [<JsonPropertyName("name")>]
          Name: string
          [<JsonPropertyName("description")>]
          Description: string
          [<JsonPropertyName("to")>]
          To: string
          [<JsonPropertyName("metadata")>]
          Metadata: MetadataItem seq
          [<JsonPropertyName("tags")>]
          Tags: string seq
          [<JsonPropertyName("documents")>]
          Documents: DocumentItem seq }

    type MapComponent =
        { [<JsonPropertyName("id")>]
          Id: string
          [<JsonPropertyName("name")>]
          Name: string
          [<JsonPropertyName("description")>]
          Description: string
          [<JsonPropertyName("x")>]
          X: int
          [<JsonPropertyName("y")>]
          Y: int
          [<JsonPropertyName("metadata")>]
          Metadata: MetadataItem seq
          [<JsonPropertyName("tags")>]
          Tags: string seq
          [<JsonPropertyName("documents")>]
          Documents: DocumentItem seq
          [<JsonPropertyName("connections")>]
          Connections: Connection seq }

    type InfrastructureMap =
        { [<JsonPropertyName("components")>]
          Components: MapComponent seq }

    module Store =

        [<RequireQualifiedAccess>]
        module Internal =
            let componentsTable =
                """
            CREATE TABLE components (
				id TEXT NOT NULL,
				name TEXT NOT NULL,
				description TEXT,
				x INTEGER,
				y INTEGER,
				CONSTRAINT components_PK PRIMARY KEY (id)
			);
            """

            let tagsTable =
                """
			CREATE TABLE tags (
				name TEXT NOT NULL,
				CONSTRAINT tags_PK PRIMARY KEY (name)
			);
			"""

            let documentsTable =
                """
			CREATE TABLE documents (
				id TEXT NOT NULL,
				name TEXT NOT NULL,
				description TEXT NOT NULL,
				document BLOB NOT NULL,
				CONSTRAINT documents_PK PRIMARY KEY (id)
			);
			"""

            let componentTagsTable =
                """
			CREATE TABLE component_tags (
				component_id TEXT NOT NULL,
				tag TEXT NOT NULL,
				CONSTRAINT component_tags_FK FOREIGN KEY (component_id) REFERENCES components(id),
				CONSTRAINT component_tags_FK_1 FOREIGN KEY (tag) REFERENCES tags(name)
			);
			"""

            let componentDocumentsTable =
                """
			CREATE TABLE component_documents (
				component_id TEXT NOT NULL,
				document_id TEXT NOT NULL,
				CONSTRAINT component_documents_FK FOREIGN KEY (component_id) REFERENCES components(id),
				CONSTRAINT component_documents_FK_1 FOREIGN KEY (document_id) REFERENCES documents(id)
			);
			"""

            let componentMetadataTable =
                """
			CREATE TABLE component_metadata (
				component_id TEXT NOT NULL,
				"key" TEXT NOT NULL,
				value TEXT NOT NULL,
				CONSTRAINT component_metadata_PK PRIMARY KEY (component_id,"key"),
				CONSTRAINT component_metadata_FK FOREIGN KEY (component_id) REFERENCES components(id)
			);
			"""

            let connectionsTable =
                """
			CREATE TABLE connections (
				id TEXT NOT NULL,
				name TEXT NOT NULL,
				description TEXT NOT NULL,
				from_id TEXT NOT NULL,
				to_id TEXT NOT NULL,
				CONSTRAINT connections_PK PRIMARY KEY (id),
				CONSTRAINT connections_FK FOREIGN KEY (from_id) REFERENCES components(id),
				CONSTRAINT connections_FK_1 FOREIGN KEY (to_id) REFERENCES components(id)
			);
			"""

            let connectionTagsTable =
                """
			CREATE TABLE connection_tags (
				connection_id TEXT NOT NULL,
				tag TEXT NOT NULL,
				CONSTRAINT connection_tags_FK FOREIGN KEY (connection_id) REFERENCES connections(id),
				CONSTRAINT connection_tags_FK_1 FOREIGN KEY (tag) REFERENCES tags(name)
			);
			"""

            let connectionDocumentsTable =
                """
			CREATE TABLE connection_documents (
				connection_id TEXT NOT NULL,
				document_id TEXT NOT NULL,
				CONSTRAINT connection_documents_FK FOREIGN KEY (connection_id) REFERENCES connections(id),
				CONSTRAINT connection_documents_FK_1 FOREIGN KEY (document_id) REFERENCES documents(id)
			);
			"""

            let connectionMetadataTable =
                """
			CREATE TABLE connection_metadata (
				connection_id TEXT NOT NULL,
				"key" TEXT NOT NULL,
				value TEXT NOT NULL,
				CONSTRAINT connections_metadata_PK PRIMARY KEY (connection_id,"key")
			);
			"""

        [<RequireQualifiedAccess>]
        module Records =

            type Tag = { Name: string }

            type Document =
                { Id: string
                  Name: string
                  Description: string
                  Document: BlobField }

            type Component =
                { Id: string
                  Name: string
                  Description: string
                  X: int
                  Y: int }

            type ComponentDocument =
                { ComponentId: string
                  DocumentId: string }

            type ComponentMetadata =
                { ComponentId: string
                  Key: string
                  Value: string }

            type ComponentTags = { ComponentId: string; Tag: string }

            type Connection =
                { Id: string
                  Name: string
                  Description: string
                  FromId: string
                  ToId: string }

            type ConnectionDocument =
                { ComponentId: string
                  DocumentId: string }

            type ConnectionMetadata =
                { ComponentId: string
                  Key: string
                  Value: string }

            type ConnectionTag = { ComponentId: string; Tag: string }

        let initialize (qh: QueryHandler) =
            [ Internal.tagsTable
              Internal.documentsTable
              Internal.componentsTable
              Internal.componentDocumentsTable
              Internal.componentMetadataTable
              Internal.componentTagsTable
              Internal.connectionsTable
              Internal.connectionDocumentsTable
              Internal.connectionMetadataTable
              Internal.connectionTagsTable ]
            |> List.map qh.ExecuteSqlNonQuery
            |> ignore

        let addTag (qh: QueryHandler) (tag: Records.Tag) = qh.Insert("tags", tag)

        let addDocument (qh: QueryHandler) (document: Records.Document) = qh.Insert("documents", document)

        let addComponent (qh: QueryHandler) (comp: Records.Component) = qh.Insert("components", comp)

        let addComponentDocument (qh: QueryHandler) (componentDocument: Records.ComponentDocument) =
            qh.Insert("component_documents", componentDocument)

        let addComponentMetadata (qh: QueryHandler) (componentMetadata: Records.ComponentMetadata) =
            qh.Insert("component_metadata", componentMetadata)

        let addComponentTag (qh: QueryHandler) (componentTag: Records.ComponentTags) =
            qh.Insert("component_tags", componentTag)

        let addConnection (qh: QueryHandler) (connection: Records.Connection) = qh.Insert("connections", connection)

        let addConnectionDocument (qh: QueryHandler) (connectionDocument: Records.ConnectionDocument) =
            qh.Insert("connection_documents", connectionDocument)

        let addConnectionMetadata (qh: QueryHandler) (connectionMetadata: Records.ConnectionMetadata) =
            qh.Insert("connection_metadata", connectionMetadata)

        let addConnectionTag (qh: QueryHandler) (connectionTag: Records.ConnectionTag) =
            qh.Insert("connection_tags", connectionTag)

        let getComponent (qh: QueryHandler) (id: string) =
            let sql =
                """
			SELECT id, name, description, x, y
			FROM components
			WHERE id = @0;
			"""

            qh.SelectSingleAnon<Records.Component>(sql, [ id ])

        let getAllComponents (qh: QueryHandler) =
            qh.Select<Records.Component>("components")

        let getDocument (qh: QueryHandler) (id: string) =
            let sql =
                """
			SELECT id, name, description, document
			FROM documents
			WHERE id = @0;
			"""

            qh.SelectSingleAnon<Records.Document>(sql, [ id ])

        let getAllTags (qh: QueryHandler) = qh.Select<Records.Tag>("tags")

        let getAllComponentMetadata (qh: QueryHandler) =
            qh.Select<Records.ComponentMetadata>("components_metadata")

        let getAllComponentTags (qh: QueryHandler) =
            qh.Select<Records.ComponentTags>("component_tags")

        let getAllComponentDocuments (qh: QueryHandler) =
            qh.Select<Records.ComponentDocument>("component_documents")

        let getAllConnections (qh: QueryHandler) =
            qh.Select<Records.Connection>("connections")

        let getAllConnectionDocuments (qh: QueryHandler) =
            qh.Select<Records.ConnectionDocument>("connection_documents")

        let getAllConnectionMetadata (qh: QueryHandler) =
            qh.Select<Records.ConnectionMetadata>("connection_metadata")

        let getAllConnectionTags (qh: QueryHandler) =
            qh.Select<Records.ConnectionTag>("connection_tags")
            