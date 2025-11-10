/* eslint-disable */
; (function (root, factory) {
    if (typeof define === 'function' && define.amd) { // AMD
        define(["./lib/blockly_compressed.js"], factory);
    } else if (typeof exports === 'object') { // Node.js
        module.exports = factory(require("./lib/blockly_compressed.js"));
    } else { // Script
        root.nqc = factory(root.Blockly);
        root.Blockly.Nqc = root.nqc.nqcGenerator;
    }
}(this, function (__parent__) {

    'use strict';

    const Blockly = __parent__;

    /**
     * Classe do gerador de código do NQC.
     * Sistema customizado de geração de código para linguagem NQC (Not Quite C)
     * usado para programar LEGO Mindstorms RCX.
     */
    class NqcGenerator extends Blockly.Generator {
        constructor() {
            super('NQC');

            // Lista de nomes ilegais para variáveis e funções em NQC
            this.addReservedWords(
                'task,sub,inline,void,int,const,asm,abs,sign,if,else,while,do,for,repeat,' +
                'switch,case,default,break,continue,until,return,monitor,catch,acquire,release,' +
                'Random,OnFwd,OnRev,Off,Float,Fwd,Rev,Toggle,SetPower,SetDirection,ClearTimer,' +
                'Timer,Wait,PlaySound,PlayTone,ClearMessage,SendMessage,Message,SetSensor,' +
                'SetSensorType,SetSensorMode,ClearSensor,Sensor,SensorValue,SensorType,SensorMode,' +
                'StartTask,StopTask,StopAllTasks,SetPriority,OUT_A,OUT_B,OUT_C,SENSOR_1,SENSOR_2,' +
                'SENSOR_3,true,false'
            );

            // Ordem de precedência dos operadores (seguindo padrão C)
            this.ORDER_ATOMIC = 0;           // 0 "" ...
            this.ORDER_UNARY_POSTFIX = 1;    // expr++ expr-- () []
            this.ORDER_UNARY_PREFIX = 2;     // ++expr --expr +expr -expr ~ !
            this.ORDER_MULTIPLICATIVE = 3;   // * / %
            this.ORDER_ADDITIVE = 4;         // + -
            this.ORDER_SHIFT = 5;            // << >>
            this.ORDER_RELATIONAL = 6;       // < <= > >=
            this.ORDER_EQUALITY = 7;         // == !=
            this.ORDER_BITWISE_AND = 8;      // &
            this.ORDER_BITWISE_XOR = 9;      // ^
            this.ORDER_BITWISE_OR = 10;      // |
            this.ORDER_LOGICAL_AND = 11;     // &&
            this.ORDER_LOGICAL_OR = 12;      // ||
            this.ORDER_CONDITIONAL = 13;     // ?:
            this.ORDER_ASSIGNMENT = 14;      // = += -= *= /= %= <<= >>= &= ^= |=
            this.ORDER_NONE = 99;            // (...)

            // Sistema customizado de rastreamento de variáveis por escopo
            // Necessário porque NQC exige declaração de variáveis locais dentro de cada task
            this.scopeVariables_ = {};
            this.currentScope_ = null;
            this.hasMainTask_ = false;
        }

        /**
         * Inicializa o gerador para um novo workspace
         */
        init(workspace) {
            super.init(workspace);

            if (!this.nameDB_) {
                this.nameDB_ = new Blockly.Names(this.RESERVED_WORDS_);
            } else {
                this.nameDB_.reset();
            }

            this.nameDB_.setVariableMap(workspace.getVariableMap());
            this.nameDB_.populateVariables(workspace);
            this.nameDB_.populateProcedures(workspace);

            // NÃO usar o sistema padrão de variáveis globais do Blockly
            // NQC exige variáveis locais dentro de cada task

            // Resetar o sistema de rastreamento de variáveis por escopo
            this.scopeVariables_ = {};
            this.currentScope_ = null;
            this.hasMainTask_ = false;
        }

        /**
         * Inicia um novo escopo (task, função, etc.)
         * @param {string} scopeId - Identificador único do escopo
         */
        startScope(scopeId) {
            this.currentScope_ = scopeId;
            if (!this.scopeVariables_[scopeId]) {
                this.scopeVariables_[scopeId] = new Set();
            }
        }

        /**
         * Finaliza o escopo atual
         */
        endScope() {
            this.currentScope_ = null;
        }

        /**
         * Registra o uso de uma variável no escopo atual
         * @param {string} varName - Nome da variável
         */
        registerVariableInScope(varName) {
            if (this.currentScope_ && varName) {
                this.scopeVariables_[this.currentScope_].add(varName);
            }
        }

        /**
         * Obtém as declarações de variáveis para um escopo
         * @param {string} scopeId - Identificador do escopo
         * @return {string} - Declarações de variáveis no formato NQC
         */
        getVariableDeclarations(scopeId) {
            const variables = this.scopeVariables_[scopeId];
            if (!variables || variables.size === 0) {
                return '';
            }

            const declarations = Array.from(variables).map(varName => `  int ${varName};`).join('\n');
            return declarations + '\n\n';
        }

        /**
         * Coleta todas as variáveis usadas em um bloco e seus filhos (recursivamente)
         * @param {Blockly.Block} block - Bloco raiz
         * @return {Set<string>} - Conjunto de nomes de variáveis
         */
        collectVariablesInBlock(block) {
            const variables = new Set();

            if (!block) {
                return variables;
            }

            // Verificar se o bloco usa variáveis
            if (block.type === 'nqc_variavel_recebe' || block.type === 'variables_set') {
                const varName = block.getFieldValue('VAR');
                if (varName) {
                    variables.add(varName);
                }
            } else if (block.type === 'nqc_valor_variavel' || block.type === 'variables_get') {
                const varName = block.getFieldValue('VAR');
                if (varName) {
                    variables.add(varName);
                }
            } else if (block.type === 'math_change') {
                const varName = block.getFieldValue('VAR');
                if (varName) {
                    variables.add(varName);
                }
            } else if (block.type === 'controls_for' || block.type === 'controls_forEach') {
                const varName = block.getFieldValue('VAR');
                if (varName) {
                    variables.add(varName);
                }
            }

            // Recursivamente coletar variáveis de blocos filhos (inputs)
            for (let i = 0; i < block.inputList.length; i++) {
                const input = block.inputList[i];
                if (input.connection && input.connection.targetBlock()) {
                    const childVars = this.collectVariablesInBlock(input.connection.targetBlock());
                    childVars.forEach(v => variables.add(v));
                }
            }

            // Recursivamente coletar variáveis de blocos seguintes
            if (block.nextConnection && block.nextConnection.targetBlock()) {
                const nextVars = this.collectVariablesInBlock(block.nextConnection.targetBlock());
                nextVars.forEach(v => variables.add(v));
            }

            return variables;
        }

        /**
         * Converte workspace em código, garantindo que tenha task main
         * @param {Blockly.Workspace} workspace - Workspace do Blockly
         * @return {string} - Código NQC completo
         */
        workspaceToCode(workspace) {
            if (!workspace) {
                return '';
            }

            this.init(workspace);

            const allBlocks = workspace.getTopBlocks(true);
            let taskCode = '';
            let looseCode = '';
            let looseBlocks = [];

            // Primeiro, processar todos os blocos
            for (let i = 0; i < allBlocks.length; i++) {
                const block = allBlocks[i];

                // Verificar se é um bloco de tarefa (principal ou nomeada)
                if (block.type === 'nqc_tarefa_principal' || block.type === 'nqc_tarefa_nomeada') {
                    const blockCode = this.blockToCode(block);
                    if (blockCode) {
                        taskCode += blockCode;
                        if (!blockCode.endsWith('\n')) {
                            taskCode += '\n';
                        }
                    }
                } else {
                    // Bloco solto - guardar para processar depois
                    looseBlocks.push(block);
                }
            }

            // Processar blocos soltos
            if (looseBlocks.length > 0) {
                // Se NÃO há task main, criar uma com os blocos soltos dentro
                if (!this.hasMainTask_) {
                    const scopeId = 'task_main';

                    // Iniciar rastreamento de escopo
                    this.startScope(scopeId);

                    // Coletar variáveis de todos os blocos soltos
                    for (let i = 0; i < looseBlocks.length; i++) {
                        this.collectVariablesInBlock(looseBlocks[i]);
                    }

                    // Gerar código dos blocos soltos
                    for (let i = 0; i < looseBlocks.length; i++) {
                        const blockCode = this.blockToCode(looseBlocks[i]);
                        if (blockCode) {
                            looseCode += '  ' + blockCode.trim().replace(/\n/g, '\n  ') + '\n';
                        }
                    }

                    // Obter declarações de variáveis
                    const varDeclarations = this.getVariableDeclarations(scopeId);

                    // Finalizar escopo
                    this.endScope();

                    // Criar task main com declarações e blocos soltos
                    taskCode += '\ntask main()\n{\n';
                    if (varDeclarations) {
                        taskCode += varDeclarations;
                    }
                    taskCode += looseCode;
                    taskCode += '}\n';
                } else {
                    // Se JÁ existe task main, gerar os blocos soltos normalmente (fora de qualquer tarefa)
                    // Isso permite código de inicialização ou definições globais
                    for (let i = 0; i < looseBlocks.length; i++) {
                        const blockCode = this.blockToCode(looseBlocks[i]);
                        if (blockCode) {
                            looseCode += blockCode;
                            if (!blockCode.endsWith('\n')) {
                                looseCode += '\n';
                            }
                        }
                    }

                    // Adicionar blocos soltos ANTES das tarefas
                    taskCode = looseCode + '\n' + taskCode;
                }
            } else if (!this.hasMainTask_) {
                // Não há blocos soltos E não há task main - criar task main vazia
                taskCode += '\ntask main()\n{\n}\n';
            }

            return this.finish(taskCode);
        }

        /**
         * Prepend the generated code with definitions.
         * @param {string} code Generated code.
         * @return {string} Completed code.
         */
        finish(code) {
            const imports = [];
            const definitions = [];
            for (let name in this.definitions_) {
                // Ignorar 'variables' do sistema padrão
                if (name === 'variables') {
                    continue;
                }

                const def = this.definitions_[name];
                if (def.match(/^#include/)) {
                    imports.push(def);
                } else {
                    definitions.push(def);
                }
            }

            const allDefs = (imports.length ? imports.join('\n') + '\n\n' : '') +
                (definitions.length ? definitions.join('\n\n') + '\n\n' : '');
            return allDefs.replace(/\n\n+/g, '\n\n').replace(/\n*$/, '\n') + code;
        }

        /**
         * Naked values are top-level blocks with outputs that aren't plugged into
         * anything. A trailing semicolon is needed to make this legal.
         * @param {string} line Line of generated code.
         * @return {string} Legal line of code.
         */
        scrubNakedValue(line) {
            return line + ';\n';
        }

        /**
         * Encode a string as a properly escaped NQC string, complete with quotes.
         * @param {string} string Text to encode.
         * @return {string} NQC string.
         */
        quote_(string) {
            string = string.replace(/\\/g, '\\\\')
                .replace(/\n/g, '\\n')
                .replace(/"/g, '\\"');
            return '"' + string + '"';
        }

        /**
         * Common tasks for generating NQC from blocks.
         * @param {!Blockly.Block} block Current block.
         * @param {string} code The NQC code created for this block.
         * @param {boolean=} opt_thisOnly True to generate code for only this statement.
         * @return {string} NQC code with comments and subsequent blocks added.
         */
        scrub_(block, code, opt_thisOnly) {
            let commentCode = '';
            if (!block.outputConnection || !block.outputConnection.targetConnection) {
                const comment = block.getCommentText();
                if (comment) {
                    comment.split('\n').forEach(function (line) {
                        commentCode += '// ' + line.trim() + '\n';
                    });
                }
                for (let i = 0; i < block.inputList.length; i++) {
                    if (block.inputList[i].type === 1) { // 1 = VALUE input type
                        const childBlock = block.inputList[i].connection && block.inputList[i].connection.targetBlock();
                        if (childBlock) {
                            const childComment = this.allNestedComments(childBlock);
                            if (childComment) {
                                commentCode += childComment;
                            }
                        }
                    }
                }
            }
            const nextBlock = block.nextConnection && block.nextConnection.targetBlock();
            const nextCode = opt_thisOnly ? '' : this.blockToCode(nextBlock);
            return commentCode + code + nextCode;
        }
    }

    // Create generator instance
    const nqcGenerator = new NqcGenerator();

    // Export the generator
    return {
        NqcGenerator: NqcGenerator,
        nqcGenerator: nqcGenerator,
        Order: {
            ATOMIC: nqcGenerator.ORDER_ATOMIC,
            UNARY_POSTFIX: nqcGenerator.ORDER_UNARY_POSTFIX,
            UNARY_PREFIX: nqcGenerator.ORDER_UNARY_PREFIX,
            MULTIPLICATIVE: nqcGenerator.ORDER_MULTIPLICATIVE,
            ADDITIVE: nqcGenerator.ORDER_ADDITIVE,
            SHIFT: nqcGenerator.ORDER_SHIFT,
            RELATIONAL: nqcGenerator.ORDER_RELATIONAL,
            EQUALITY: nqcGenerator.ORDER_EQUALITY,
            BITWISE_AND: nqcGenerator.ORDER_BITWISE_AND,
            BITWISE_XOR: nqcGenerator.ORDER_BITWISE_XOR,
            BITWISE_OR: nqcGenerator.ORDER_BITWISE_OR,
            LOGICAL_AND: nqcGenerator.ORDER_LOGICAL_AND,
            LOGICAL_OR: nqcGenerator.ORDER_LOGICAL_OR,
            CONDITIONAL: nqcGenerator.ORDER_CONDITIONAL,
            ASSIGNMENT: nqcGenerator.ORDER_ASSIGNMENT,
            NONE: nqcGenerator.ORDER_NONE
        }
    };

}));
