using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Enemy AI Behavior Graph를 자동으로 구성하는 에디터 유틸리티.
/// Menu: Tools > Enemy AI > Build Behavior Tree
/// </summary>
public static class BehaviorTreeBuilder
{
    private const string GraphPath      = "Assets/Scripts/Player/Behavior Graph.asset";
    private const string AgentVarName   = "Agent";

    // ── 캐시된 타입들 ──────────────────────────────────────────────────────────
    private static Type _compositeNodeModelType;
    private static Type _actionNodeModelType;
    private static Type _conditionalGuardNodeModelType;
    private static Type _conditionModelType;
    private static Type _fieldModelType;
    private static Type _serializableTypeType;
    private static Type _selectorCompositeType;
    private static Type _sequenceCompositeType;
    private static Type _portModelType;
    private static Type _typedVariableModelOpenType;

    [MenuItem("Tools/Enemy AI/Build Behavior Tree")]
    public static void BuildBehaviorTree()
    {
        ResolveTypes();
        if (!ValidateTypes()) return;

        // ── 1. 에셋 로드 ──────────────────────────────────────────────────────
        var graphSO = AssetDatabase.LoadAssetAtPath<ScriptableObject>(GraphPath);
        if (graphSO == null)
        {
            Debug.LogError($"[BehaviorTreeBuilder] 그래프 에셋을 찾을 수 없습니다: {GraphPath}");
            return;
        }
        var graphType = graphSO.GetType();

        // ── 2. Blackboard 변수 "Agent" 확보 ──────────────────────────────────
        var bbAsset   = graphType.GetProperty("MainBlackboardAuthoringAsset")?.GetValue(graphSO);
        var agentVar  = EnsureBlackboardVariable(bbAsset, AgentVarName, typeof(GameObject));
        if (agentVar == null) { Debug.LogError("[BehaviorTreeBuilder] Blackboard 변수 생성 실패"); return; }

        // ── 3. 기존 노드 정리 (Start 제외) ───────────────────────────────────
        var nodeList = graphType.GetProperty("Nodes")?.GetValue(graphSO) as IList<object>;
        if (nodeList == null)
        {
            // IList<object>로 캐스팅 안 될 경우 non-generic IList 사용
            var rawList = graphType.GetProperty("Nodes")?.GetValue(graphSO) as System.Collections.IList;
            ClearNonStartNodes(graphSO, graphType, rawList);
            nodeList = null;
        }
        else
        {
            var toDelete = nodeList.Skip(1).ToList();
            var deleteMethod = graphType.GetMethod("DeleteNode");
            foreach (var n in toDelete) deleteMethod?.Invoke(graphSO, new[] { n });
        }

        // ── 4. 노드 생성 ─────────────────────────────────────────────────────
        var createNode  = graphType.GetMethod("CreateNode");
        var connectEdge = graphType.GetMethod("ConnectEdge");

        // Selector (Start → Selector) — BehaviorGraphAgent가 완료 후 자동 재시작
        var startNode    = GetStartNode(graphSO, graphType);
        var selectorNode = CreateComposite(graphSO, createNode, _selectorCompositeType, new Vector2(300, 0));
        ConnectNodes(graphSO, connectEdge, startNode, selectorNode);

        // ── Branch A: Chase/Attack ────────────────────────────────────────────
        float yA = -180f;
        var guardA = CreateConditionalGuard(graphSO, createNode, agentVar, "Chase/Attack", new Vector2(700, yA));
        ConnectNodes(graphSO, connectEdge, selectorNode, guardA);
        var seqA   = CreateComposite(graphSO, createNode, _sequenceCompositeType, new Vector2(950, yA));
        ConnectNodes(graphSO, connectEdge, guardA, seqA);
        var moveA  = CreateAction(graphSO, createNode, typeof(MoveToPlayerBTAction),  new Vector2(1200, yA + 60),  agentVar);
        var atkA   = CreateAction(graphSO, createNode, typeof(AttackPlayerBTAction),  new Vector2(1200, yA - 60),  agentVar,
                         ("ExtraCooldown", typeof(float), 0f));
        ConnectNodes(graphSO, connectEdge, seqA, moveA);
        ConnectNodes(graphSO, connectEdge, seqA, atkA);

        // ── Branch B: Evade/Recover ───────────────────────────────────────────
        float yB = 0f;
        var guardB = CreateConditionalGuard(graphSO, createNode, agentVar, "Evade/Recover", new Vector2(700, yB));
        ConnectNodes(graphSO, connectEdge, selectorNode, guardB);
        var seqB   = CreateComposite(graphSO, createNode, _sequenceCompositeType, new Vector2(950, yB));
        ConnectNodes(graphSO, connectEdge, guardB, seqB);
        var safeB  = CreateAction(graphSO, createNode, typeof(MoveToSafeBTAction),    new Vector2(1200, yB + 60),  agentVar,
                         ("SafeDistance", typeof(float), 9f));
        var obsB   = CreateAction(graphSO, createNode, typeof(ObservePlayerBTAction), new Vector2(1200, yB - 60),  agentVar,
                         ("Duration", typeof(float), 2f));
        ConnectNodes(graphSO, connectEdge, seqB, safeB);
        ConnectNodes(graphSO, connectEdge, seqB, obsB);

        // ── Branch C: Ambush ──────────────────────────────────────────────────
        float yC = 180f;
        var guardC = CreateConditionalGuard(graphSO, createNode, agentVar, "Ambush", new Vector2(700, yC));
        ConnectNodes(graphSO, connectEdge, selectorNode, guardC);
        var seqC   = CreateComposite(graphSO, createNode, _sequenceCompositeType, new Vector2(950, yC));
        ConnectNodes(graphSO, connectEdge, guardC, seqC);
        var ambC   = CreateAction(graphSO, createNode, typeof(MoveToAmbushBTAction),  new Vector2(1200, yC + 60),  agentVar);
        var atkC   = CreateAction(graphSO, createNode, typeof(AttackPlayerBTAction),  new Vector2(1200, yC - 60),  agentVar,
                         ("ExtraCooldown", typeof(float), 0.5f));
        ConnectNodes(graphSO, connectEdge, seqC, ambC);
        ConnectNodes(graphSO, connectEdge, seqC, atkC);

        // ── 5. 런타임 그래프 빌드 + 저장 ─────────────────────────────────────
        graphType.GetMethod("BuildRuntimeGraph")?.Invoke(graphSO, null);
        graphType.GetMethod("SaveAsset")?.Invoke(graphSO, null);
        EditorUtility.SetDirty(graphSO);
        AssetDatabase.SaveAssets();

        // ── 6. 씬 적 오브젝트에 BehaviorGraphAgent 추가 ──────────────────────
        SetupEnemyAgents(graphSO, agentVar);

        Debug.Log("[BehaviorTreeBuilder] Behavior Tree 빌드 완료!");
    }

    // ── 헬퍼: Blackboard 변수 확보 ──────────────────────────────────────────────
    private static object EnsureBlackboardVariable(object bbAsset, string varName, Type varType)
    {
        if (bbAsset == null) return null;
        var bbType    = bbAsset.GetType();
        var variables = bbType.GetProperty("Variables")?.GetValue(bbAsset) as System.Collections.IList;
        if (variables == null) return null;

        // 이미 존재하는지 확인
        foreach (var v in variables)
        {
            var nameField = v.GetType().GetField("Name");
            if (nameField?.GetValue(v)?.ToString() == varName) return v;
        }

        // 새 TypedVariableModel<T> 생성
        var typedVarModelType = _typedVariableModelOpenType.MakeGenericType(varType);
        var varModel          = Activator.CreateInstance(typedVarModelType);
        varModel.GetType().GetField("Name")?.SetValue(varModel, varName);
        varModel.GetType().GetField("IsExposed")?.SetValue(varModel, true);

        // ID 생성 (SerializableGUID)
        var guidType = varModel.GetType().GetField("ID")?.FieldType;
        if (guidType != null)
        {
            var guidCtor = guidType.GetConstructor(new[] { typeof(string) });
            if (guidCtor != null)
                varModel.GetType().GetField("ID")?.SetValue(varModel, guidCtor.Invoke(new object[] { Guid.NewGuid().ToString("N") }));
        }

        variables.Add(varModel);
        bbType.GetMethod("SetAssetDirty")?.Invoke(bbAsset, null);
        return varModel;
    }

    // ── 헬퍼: Composite 노드 생성 (Sequence 또는 Selector) ─────────────────────
    private static object CreateComposite(object graph, MethodInfo createNode, Type compositeType, Vector2 pos)
    {
        var node = createNode.Invoke(graph, new object[] { _compositeNodeModelType, pos, null, null });
        if (node == null) return null;
        var serType = Activator.CreateInstance(_serializableTypeType, compositeType);
        node.GetType().GetField("NodeType")?.SetValue(node, serType);
        return node;
    }

    // ── 헬퍼: ConditionalGuard 노드 생성 ────────────────────────────────────────
    private static object CreateConditionalGuard(object graph, MethodInfo createNode,
        object agentVar, string branchName, Vector2 pos)
    {
        var guardNode = createNode.Invoke(graph, new object[] { _conditionalGuardNodeModelType, pos, null, null });
        if (guardNode == null) return null;

        // ConditionModel 생성 및 설정
        var condModel = Activator.CreateInstance(_conditionModelType);
        var condType  = typeof(IsActiveBranchBTCondition);
        var serType   = Activator.CreateInstance(_serializableTypeType, condType);
        condModel.GetType().GetField("ConditionType")?.SetValue(condModel, serType);

        // FieldModel: Agent (BB 연결)
        var agentField = MakeLinkedFieldModel("Agent", typeof(GameObject), agentVar);
        // FieldModel: BranchName (로컬 string 값)
        var branchField = MakeLocalFieldModel("BranchName", typeof(string), branchName);

        var condFields = condModel.GetType().GetField("m_FieldValues")?.GetValue(condModel) as System.Collections.IList;
        if (condFields == null)
        {
            var listType = typeof(List<>).MakeGenericType(_fieldModelType);
            condFields = (System.Collections.IList)Activator.CreateInstance(listType);
            condModel.GetType().GetField("m_FieldValues")?.SetValue(condModel, condFields);
        }
        if (agentField  != null) condFields.Add(agentField);
        if (branchField != null) condFields.Add(branchField);

        // NodeModel 참조 설정
        condModel.GetType().GetField("NodeModel")?.SetValue(condModel, guardNode);

        // guardNode.ConditionModels 에 추가
        var condModels = guardNode.GetType()
            .GetField("<ConditionModels>k__BackingField", BindingFlags.NonPublic | BindingFlags.Instance)
            ?.GetValue(guardNode) as System.Collections.IList;
        if (condModels == null)
        {
            var listType = typeof(List<>).MakeGenericType(_conditionModelType);
            condModels = (System.Collections.IList)Activator.CreateInstance(listType);
            guardNode.GetType()
                .GetField("<ConditionModels>k__BackingField", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(guardNode, condModels);
        }
        condModels.Add(condModel);

        return guardNode;
    }

    // ── 헬퍼: Action 노드 생성 ───────────────────────────────────────────────────
    private static object CreateAction(object graph, MethodInfo createNode,
        Type actionType, Vector2 pos, object agentVar,
        params (string name, Type type, object val)[] extraFields)
    {
        var node = createNode.Invoke(graph, new object[] { _actionNodeModelType, pos, null, null });
        if (node == null) return null;

        var serType = Activator.CreateInstance(_serializableTypeType, actionType);
        node.GetType().GetField("NodeType")?.SetValue(node, serType);

        var fieldValues = node.GetType().GetField("m_FieldValues")?.GetValue(node) as System.Collections.IList;
        if (fieldValues == null)
        {
            var listType = typeof(List<>).MakeGenericType(_fieldModelType);
            fieldValues = (System.Collections.IList)Activator.CreateInstance(listType);
            node.GetType().GetField("m_FieldValues")?.SetValue(node, fieldValues);
        }

        // Agent 필드 (BB 연결)
        var agentField = MakeLinkedFieldModel("Agent", typeof(GameObject), agentVar);
        if (agentField != null) fieldValues.Add(agentField);

        // 추가 파라미터 (로컬 값)
        foreach (var (name, type, val) in extraFields)
        {
            var fm = MakeLocalFieldModel(name, type, val);
            if (fm != null) fieldValues.Add(fm);
        }

        return node;
    }

    // ── 헬퍼: FieldModel (BB 연결) ───────────────────────────────────────────────
    private static object MakeLinkedFieldModel(string fieldName, Type fieldType, object variableModel)
    {
        if (_fieldModelType == null) return null;
        var fm = Activator.CreateInstance(_fieldModelType);
        var serType = Activator.CreateInstance(_serializableTypeType, fieldType);
        fm.GetType().GetField("FieldName")?.SetValue(fm, fieldName);
        fm.GetType().GetField("Type")?.SetValue(fm, serType);
        fm.GetType().GetField("LinkedVariable")?.SetValue(fm, variableModel);
        return fm;
    }

    // ── 헬퍼: FieldModel (로컬 값) ───────────────────────────────────────────────
    private static object MakeLocalFieldModel(string fieldName, Type fieldType, object value)
    {
        if (_fieldModelType == null) return null;
        var fm = Activator.CreateInstance(_fieldModelType);
        var serType = Activator.CreateInstance(_serializableTypeType, fieldType);
        fm.GetType().GetField("FieldName")?.SetValue(fm, fieldName);
        fm.GetType().GetField("Type")?.SetValue(fm, serType);

        // BlackboardVariable<T> 생성
        var bbVarType = typeof(Unity.Behavior.BlackboardVariable<>).MakeGenericType(fieldType);
        var bbVar     = Activator.CreateInstance(bbVarType);
        bbVarType.GetProperty("Value")?.SetValue(bbVar, value);
        fm.GetType().GetField("LocalValue")?.SetValue(fm, bbVar);
        return fm;
    }

    // ── 헬퍼: 두 노드를 OutputPort → InputPort 연결 ──────────────────────────────
    private static void ConnectNodes(object graph, MethodInfo connectEdge, object fromNode, object toNode)
    {
        if (fromNode == null || toNode == null) return;
        var outPort = GetPort(fromNode, isOutput: true);
        var inPort  = GetPort(toNode,   isOutput: false);
        if (outPort == null || inPort == null)
        {
            Debug.LogWarning($"[BehaviorTreeBuilder] 포트 없음: {fromNode?.GetType().Name} → {toNode?.GetType().Name}");
            return;
        }
        connectEdge.Invoke(graph, new[] { outPort, inPort });
    }

    // ── 헬퍼: 노드에서 Input 또는 Output 포트 가져오기 ───────────────────────────
    private static object GetPort(object node, bool isOutput)
    {
        var ports = node?.GetType().GetProperty("AllPortModels")?.GetValue(node) as System.Collections.IEnumerable;
        if (ports == null) return null;
        foreach (var p in ports)
        {
            var flowField = p.GetType().GetField("m_PortDataFlowType",
                BindingFlags.NonPublic | BindingFlags.Instance);
            var val = flowField?.GetValue(p);
            if (val == null) continue;
            int intVal = Convert.ToInt32(val);
            // Output=1, Input=0 (PortDataFlowType enum)
            if (isOutput && intVal == 1) return p;
            if (!isOutput && intVal == 0) return p;
        }
        return null;
    }

    // ── 헬퍼: Start 노드 가져오기 ────────────────────────────────────────────────
    private static object GetStartNode(object graph, Type graphType)
    {
        var rawList = graphType.GetProperty("Nodes")?.GetValue(graph) as System.Collections.IList;
        return rawList?[0];
    }

    // ── 헬퍼: Start 이외 노드 삭제 ───────────────────────────────────────────────
    private static void ClearNonStartNodes(object graph, Type graphType, System.Collections.IList nodes)
    {
        if (nodes == null) return;
        var deleteMethod = graphType.GetMethod("DeleteNode");
        var toDelete = new List<object>();
        for (int i = 1; i < nodes.Count; i++) toDelete.Add(nodes[i]);
        foreach (var n in toDelete) deleteMethod?.Invoke(graph, new[] { n });
    }

    // ── 6단계: 씬 적 오브젝트 설정 ──────────────────────────────────────────────
    private static void SetupEnemyAgents(ScriptableObject graphSO, object agentVarModel)
    {
        var behaviorAgentType = Type.GetType("Unity.Behavior.BehaviorGraphAgent, Unity.Behavior");
        if (behaviorAgentType == null)
        {
            Debug.LogError("[BehaviorTreeBuilder] BehaviorGraphAgent 타입을 찾을 수 없습니다.");
            return;
        }

        // NFBTEnemyAI가 있는 모든 오브젝트 찾기
        var enemies = GameObject.FindObjectsByType<NFBTEnemyAI>(FindObjectsSortMode.None);
        foreach (var enemy in enemies)
        {
            // BehaviorGraphAgent 없으면 추가
            var agent = enemy.GetComponent(behaviorAgentType)
                        ?? enemy.gameObject.AddComponent(behaviorAgentType);

            // Graph 할당
            var graphProp = behaviorAgentType.GetProperty("Graph")
                          ?? behaviorAgentType.GetProperty("BehaviorGraph");
            graphProp?.SetValue(agent, graphSO);

            // BlackboardReference.SetVariableValue("Agent", gameObject)
            // 런타임에 BehaviorGraphAgent가 자동으로 Self를 설정하므로
            // 여기서는 그래프 연결만 해도 충분합니다.

            EditorUtility.SetDirty(enemy.gameObject);
            Debug.Log($"[BehaviorTreeBuilder] {enemy.name} → BehaviorGraphAgent 설정 완료");
        }

        // 씬 저장
        UnityEditor.SceneManagement.EditorSceneManager.MarkAllScenesDirty();
    }

    // ── 타입 해석 ─────────────────────────────────────────────────────────────────
    private static void ResolveTypes()
    {
        var allTypes = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(a => { try { return a.GetTypes(); } catch { return Type.EmptyTypes; } });

        _compositeNodeModelType        = allTypes.FirstOrDefault(t => t.FullName == "Unity.Behavior.CompositeNodeModel");
        _actionNodeModelType           = allTypes.FirstOrDefault(t => t.FullName == "Unity.Behavior.ActionNodeModel");
        _conditionalGuardNodeModelType = allTypes.FirstOrDefault(t => t.FullName == "Unity.Behavior.ConditionalGuardNodeModel");
        _conditionModelType            = allTypes.FirstOrDefault(t => t.FullName == "Unity.Behavior.ConditionModel");
        _fieldModelType                = allTypes.FirstOrDefault(t => t.Name == "FieldModel"
                                             && t.Namespace != null && t.Namespace.StartsWith("Unity.Behavior"));
        _serializableTypeType          = allTypes.FirstOrDefault(t => t.FullName == "Unity.Behavior.GraphFramework.SerializableType");
        _selectorCompositeType         = allTypes.FirstOrDefault(t => t.FullName == "Unity.Behavior.SelectorComposite");
        _sequenceCompositeType         = allTypes.FirstOrDefault(t => t.FullName == "Unity.Behavior.SequenceComposite");
        _portModelType                 = allTypes.FirstOrDefault(t => t.FullName == "Unity.Behavior.GraphFramework.PortModel");
        _typedVariableModelOpenType    = allTypes.FirstOrDefault(t =>
            t.IsGenericTypeDefinition && t.Name == "TypedVariableModel`1"
            && t.Namespace != null && t.Namespace.StartsWith("Unity.Behavior"));
    }

    private static bool ValidateTypes()
    {
        bool ok = true;
        void Check(Type t, string name)
        {
            if (t == null) { Debug.LogError($"[BehaviorTreeBuilder] 타입 없음: {name}"); ok = false; }
        }
        Check(_compositeNodeModelType,        "CompositeNodeModel");
        Check(_actionNodeModelType,           "ActionNodeModel");
        Check(_conditionalGuardNodeModelType, "ConditionalGuardNodeModel");
        Check(_conditionModelType,            "ConditionModel");
        Check(_fieldModelType,                "FieldModel");
        Check(_serializableTypeType,          "SerializableType");
        Check(_selectorCompositeType,         "SelectorComposite");
        Check(_sequenceCompositeType,         "SequenceComposite");
        Check(_typedVariableModelOpenType,    "TypedVariableModel`1");
        return ok;
    }
}
